using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;
using Microsoft.Psi;
using Microsoft.Psi.Components;
using Helpers;
using Microsoft.Azure.Kinect.BodyTracking;
using MathNet.Numerics.Statistics;

namespace Bodies
{
    public class BodiesSelectionConfiguration
    {
        /// <summary>
        /// Gets or sets in which space the bodies are sent.
        /// </summary>
        public Matrix<double>? Camera2ToCamera1Transformation { get; set; } = null;

        /// <summary>
        /// Gets or sets in which space the bodies are sent.
        /// </summary>
        public JointId JointUsedForCorrespondence { get; set; } = JointId.Pelvis;

        /// <summary>
        /// Gets or sets maximum acceptable distance for correpondance in millimeter
        /// </summary>
        public double MaxDistance { get; set; } = 0.8;

    }
    public class BodiesSelection : Subpipeline
    {
        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<List<SimplifiedBody>> OutBodiesCalibrated{ get; private set; }

        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<List<uint>> OutBodiesRemoved { get; private set; }

        /// <summary>
        /// Gets the connector of lists of currently tracked bodies.
        /// </summary>
        private Connector<Matrix<double>> InCalibrationMatrixConnector;

        /// <summary>
        /// Receiver that encapsulates the input list of Nuitrack skeletons
        /// </summary>
        public Receiver<Matrix<double>> InCalibrationMatrix => InCalibrationMatrixConnector.In;

        /// <summary>
        /// Gets the connector of lists of currently tracked bodies.
        /// </summary>
        private Connector<List<SimplifiedBody>> InCamera1BodiesConnector;

        /// <summary>
        /// Receiver that encapsulates the input list of simplified skeletons from first camera.
        /// </summary>
        public Receiver<List<SimplifiedBody>> InCamera1Bodies => InCamera1BodiesConnector.In;

        /// <summary>
        /// Gets the connector of lists of currently tracked bodies.
        /// </summary>
        private Connector<List<SimplifiedBody>> InCamera2BodiesConnector;

        /// <summary>
        /// Receiver that encapsulates the input list  of simplified skeletons from second camera
        /// </summary>
        public Receiver<List<SimplifiedBody>> InCamera2Bodies => InCamera2BodiesConnector.In;

        /// <summary>
        /// Gets the connector of lists of currently tracked bodies.
        /// </summary>
        private Connector<List<LearnedBody>> InCamera1LearnedBodiesConnector;

        /// <summary>
        /// Receiver that encapsulates the input list of learned skeletons from first camera.
        /// </summary>
        public Receiver<List<LearnedBody>> InCamera1LearnedBodies => InCamera1LearnedBodiesConnector.In;

        /// <summary>
        /// Gets the connector of new learned bodies from second camera..
        /// </summary>
        private Connector<List<LearnedBody>> InCamera2LearnedBodiesConnector;

        /// <summary>
        /// Receiver that encapsulates the input list of learned skeletons from second camera.
        /// </summary>
        public Receiver<List<LearnedBody>> InCamera2LearnedBodies => InCamera2LearnedBodiesConnector.In;

        /// <summary>
        /// Gets the connector of lists of removed skeletons from first camera.
        /// </summary>
        private Connector<List<uint>> InCamera1RemovedBodiesConnector;

        /// <summary>
        /// Receiver that encapsulates the input list of removed skeletons
        /// </summary>
        public Receiver<List<uint>> InCamera1RemovedBodies => InCamera1RemovedBodiesConnector.In;

        /// <summary>
        /// Gets the nuitrack connector of lists of removed bodies.
        /// </summary>
        private Connector<List<uint>> InCamera2RemovedBodiesConnector;

        /// <summary>
        /// Receiver that encapsulates the input list of removed skeletons
        /// </summary>
        public Receiver<List<uint>> InCamera2RemovedBodies => InCamera2RemovedBodiesConnector.In;

        private BodiesSelectionConfiguration Configuration { get; }

        private List<Tuple<uint, uint>> CorrespondanceList = new List<Tuple<uint, uint>>();
        private List<Tuple<uint, uint>> NotCorrespondanceList = new List<Tuple<uint, uint>>();

        private Dictionary<(uint, uint), uint> GeneratedIdsMap = new Dictionary<(uint, uint), uint>();

        private Dictionary<uint, LearnedBody> Camera1LearnedBodies = new Dictionary<uint, LearnedBody>();
        private Dictionary<uint, LearnedBody> Camera2LearnedBodies = new Dictionary<uint, LearnedBody>();
        private uint idCount = 1;
        private enum TupleState { AlreadyExist, KeyAlreadyInserted, GoodToInsert, LeftJokerFound, RightJokerFound  };

        public BodiesSelection(Pipeline parent, BodiesSelectionConfiguration? configuration = null, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null)
          : base(parent, name, defaultDeliveryPolicy)
        {
            Configuration = configuration ?? new BodiesSelectionConfiguration();

            InCamera1BodiesConnector = CreateInputConnectorFrom<List<SimplifiedBody>>(parent, nameof(InCamera1BodiesConnector));
            InCamera2BodiesConnector = CreateInputConnectorFrom<List<SimplifiedBody>>(parent, nameof(InCamera2BodiesConnector));
            InCamera1LearnedBodiesConnector = CreateInputConnectorFrom<List<LearnedBody>>(parent, nameof(InCamera1LearnedBodiesConnector));
            InCamera2LearnedBodiesConnector = CreateInputConnectorFrom<List<LearnedBody>>(parent, nameof(InCamera2LearnedBodiesConnector));
            InCamera1RemovedBodiesConnector = CreateInputConnectorFrom<List<uint>>(parent, nameof(InCamera1RemovedBodiesConnector));
            InCamera2RemovedBodiesConnector = CreateInputConnectorFrom<List<uint>>(parent, nameof(InCamera2RemovedBodiesConnector));
            InCalibrationMatrixConnector = CreateInputConnectorFrom<Matrix<double>>(parent, nameof(InCalibrationMatrixConnector));
            OutBodiesCalibrated = parent.CreateEmitter<List<SimplifiedBody>>(this, nameof(OutBodiesCalibrated));
            OutBodiesRemoved = parent.CreateEmitter<List<uint>>(this, nameof(OutBodiesRemoved));

            if (Configuration.Camera2ToCamera1Transformation == null)
                InCamera1BodiesConnector.Pair(InCamera2BodiesConnector).Out.Fuse(InCalibrationMatrixConnector.Out, Available.Nearest<Matrix<double>>()).Do(Process);
            else
                InCamera1BodiesConnector.Pair(InCamera2BodiesConnector).Do(Process);

            InCamera1LearnedBodiesConnector.Do(LearnedBodyProcessing1);
            InCamera2LearnedBodiesConnector.Do(LearnedBodyProcessing2);

            InCamera1RemovedBodiesConnector.Do(RemovedBodyProcessing1);
            InCamera2RemovedBodiesConnector.Do(RemovedBodyProcessing2);
        }
        private void Process((List<SimplifiedBody>, List<SimplifiedBody>, Matrix<double>) bodies, Envelope envelope)
        {
            Configuration.Camera2ToCamera1Transformation = bodies.Item3;
            Process((bodies.Item1, bodies.Item2), envelope);
        }

        private void Process((List<SimplifiedBody>, List<SimplifiedBody>) bodies, Envelope envelope)
        {
            Dictionary<uint, SimplifiedBody> dicsC1 = new Dictionary<uint, SimplifiedBody>(), dicsC2 = new Dictionary<uint, SimplifiedBody>();
            UpdateCorrespondanceMap(bodies.Item1, bodies.Item2, ref dicsC1, ref dicsC2);
            var bbody = SelectBestBody(dicsC1, dicsC2);
            OutBodiesCalibrated.Post(bbody, envelope.OriginatingTime);
            //OutBodiesCalibrated.Post(SelectBestBody(dicsC1, dicsC2), envelope.OriginatingTime);
        }

        private void LearnedBodyProcessing1(List<LearnedBody> list, Envelope envelope)
        {
            LearnedBodyProcessing(list, ref Camera1LearnedBodies);
        }
        private void LearnedBodyProcessing2(List<LearnedBody> list, Envelope envelope)
        {
            LearnedBodyProcessing(list, ref Camera2LearnedBodies);
        }
        private void LearnedBodyProcessing(List<LearnedBody> list, ref Dictionary<uint, LearnedBody> dic)
        { 
            foreach (var item in list)
            {
                if (dic.ContainsKey(item.Id))
                    continue;
                dic.Add(item.Id, item);
            }
        }

        private void RemovedBodyProcessing1(List<uint> list, Envelope envelope)
        {
            RemovedBodyProcessing(list, true, ref Camera1LearnedBodies, envelope);
        }

        private void RemovedBodyProcessing2(List<uint> list, Envelope envelope)
        {
            RemovedBodyProcessing(list, false, ref Camera2LearnedBodies, envelope);
        }

        private void RemovedBodyProcessing(List<uint> list, bool isMaster, ref Dictionary<uint, LearnedBody> dic, Envelope envelope)
        {
            lock(this)
            {
                List<uint> removedId = new List<uint>();
                foreach (uint id in list)
                {
                    if (dic.ContainsKey(id))
                        dic.Remove(id);
                    Tuple<uint, uint> tuple, ouTuple;
                    if (isMaster)
                        tuple = new Tuple<uint, uint>(id, 0);
                    else
                        tuple = new Tuple<uint, uint>(0, id);
                    if (KeyOrValueExistInList(tuple, out ouTuple) != 0)
                    {
                        removedId.Add(GeneratedIdsMap[(ouTuple.Item1, ouTuple.Item2)]);
                        GeneratedIdsMap.Remove((ouTuple.Item1, ouTuple.Item2));
                        CorrespondanceList.Remove(ouTuple);
                    }
                }
                OutBodiesRemoved.Post(removedId, envelope.OriginatingTime);
            }
        }

        private void UpdateCorrespondanceMap(List<SimplifiedBody> camera1, List<SimplifiedBody> camera2, ref Dictionary<uint, SimplifiedBody> d1, ref Dictionary<uint, SimplifiedBody> d2)
        {
            var newMapping = ComputeCorrespondenceMap(camera1, camera2, ref d1, ref d2);

            //checking consistancy with old mapping
            foreach (var iterator in newMapping)
            {
                Tuple<uint, uint> tuple;
                switch (KeyOrValueExistInList(iterator, out tuple))
                {
                    case TupleState.AlreadyExist:
                        break;
                    case TupleState.KeyAlreadyInserted:
                        FindCorrectPairFromBones(ref d1, ref d2, iterator, tuple);
                        break;
                    case TupleState.GoodToInsert:
                        CorrespondanceList.Add(iterator);
                        GeneratedIdsMap[(iterator.Item1, iterator.Item2)] = idCount++;
                        break;
                    case TupleState.RightJokerFound:
                        if (tuple.Item2 != 0)
                            IntegrateInDicsAndList(iterator, tuple);
                        break;
                    case TupleState.LeftJokerFound:
                        if (tuple.Item1 != 0)
                            IntegrateInDicsAndList(iterator, tuple);
                        break;
                }
            }
        }

        private void IntegrateInDicsAndList(Tuple<uint, uint> old, Tuple<uint, uint> newItem)
        {
            CorrespondanceList.Remove(newItem);
            CorrespondanceList.Add(new Tuple<uint, uint>(newItem.Item1, newItem.Item2));
            if (GeneratedIdsMap.ContainsKey((old.Item1, old.Item2)))
            {
                GeneratedIdsMap[(newItem.Item1, newItem.Item2)] = GeneratedIdsMap[(old.Item1, old.Item2)];
                GeneratedIdsMap.Remove((old.Item1, old.Item2));
            }
            else if(!GeneratedIdsMap.ContainsKey((newItem.Item1, newItem.Item2)))
                GeneratedIdsMap[(newItem.Item1, newItem.Item2)] = idCount++;
        }

        private void FindCorrectPairFromBones(ref Dictionary<uint, SimplifiedBody> d1, ref Dictionary<uint, SimplifiedBody> d2, Tuple<uint, uint> iterator, Tuple<uint, uint> tuple)
        {
            LearnedBody unique, p1, p2;
            if ((iterator.Item2 + iterator.Item1 + tuple.Item1 + tuple.Item2) == 0)
            {
                uint i = 0;
                i++;
            }
            if (iterator.Item1 == tuple.Item1)
            {
                p1 = Camera2LearnedBodies[iterator.Item2];
                p2 = Camera2LearnedBodies[tuple.Item2];
                unique = Camera1LearnedBodies[iterator.Item1];
            }
            else if (iterator.Item2 == tuple.Item2)
            {
                p1 = Camera2LearnedBodies[iterator.Item2];
                p2 = Camera2LearnedBodies[tuple.Item2];
                unique = Camera1LearnedBodies[iterator.Item1];
            }
            else
            {
                throw new Exception("oups");
            }

            List<double> dist1 = new List<double>(), dist2 = new List<double>();
            foreach (var bones in unique.LearnedBones)
            {
                if (bones.Value == 0.0)
                    continue;
                if (p1.LearnedBones[bones.Key] > 0.0)
                    dist1.Add(Math.Abs(p1.LearnedBones[bones.Key] - bones.Value));
                if (p2.LearnedBones[bones.Key] > 0.0)
                    dist2.Add(Math.Abs(p2.LearnedBones[bones.Key] - bones.Value));
            }

            var statistics1 = Statistics.MeanStandardDeviation(dist1);
            var statistics2 = Statistics.MeanStandardDeviation(dist2);
            
            if (statistics1.Item2 < statistics2.Item2)
                IntegrateInDicsAndList(tuple, iterator);
            else
                IntegrateInDicsAndList(iterator, tuple);
        }

        private List<Tuple<uint, uint>> ComputeCorrespondenceMap(List<SimplifiedBody> camera1, List<SimplifiedBody> camera2, ref Dictionary<uint, SimplifiedBody> d1, ref Dictionary<uint, SimplifiedBody> d2) 
        {
            if (camera1.Count == 0 || camera2.Count == 0 || Configuration.Camera2ToCamera1Transformation == null) 
                return new List<Tuple<uint, uint>>();

            // Bruteforce ftm, might simplify to check directly the max allowed distance.
            Dictionary<uint, List<Tuple<double, uint>>> distances = new Dictionary<uint, List<Tuple<double, uint>>>();
           
            foreach (SimplifiedBody bodyC1 in camera1)
            {
                d1[bodyC1.Id] = bodyC1;
                distances[bodyC1.Id] = new List<Tuple<double, uint>>();
                foreach (SimplifiedBody bodyC2 in camera2)
                    distances[bodyC1.Id].Add(new Tuple<double, uint>(MathNet.Numerics.Distance.Euclidean(bodyC1.Joints[Configuration.JointUsedForCorrespondence].Item2.ToVector(), Helpers.Helpers.CalculateTransform(bodyC2.Joints[Configuration.JointUsedForCorrespondence].Item2, Configuration.Camera2ToCamera1Transformation).ToVector()), bodyC2.Id));
            }

            List<Tuple<uint, uint>> correspondanceMap = new List<Tuple<uint, uint>>();
            List<uint> notMissingC2 = new List<uint>();
            foreach(var iterator in distances)
            { 
                iterator.Value.Sort(new TupleDoubleUintComparer());
                //to check if sort is good
                if (iterator.Value.First().Item1 < Configuration.MaxDistance)
                {
                    correspondanceMap.Add(new Tuple<uint, uint>(iterator.Key, iterator.Value.First().Item2));
                    notMissingC2.Add(iterator.Value.First().Item2);
                }
                else
                    correspondanceMap.Add(new Tuple<uint, uint>(iterator.Key, 0));
            }
            foreach(SimplifiedBody bodyC2 in camera2)
            {
                d2[bodyC2.Id] = bodyC2;
                if (!notMissingC2.Contains(bodyC2.Id))
                    correspondanceMap.Add(new Tuple<uint, uint>(0, bodyC2.Id));
            }
            return correspondanceMap;
        }

        private void SelectByConfidence(Dictionary<uint, SimplifiedBody> camera1, Dictionary<uint, SimplifiedBody> camera2, Tuple<uint,uint> ids, ref List<SimplifiedBody> bestBodies)
        {
            if (AccumulatedConfidence(camera1[ids.Item1]) < AccumulatedConfidence(camera2[ids.Item2]))
            {
                SimplifiedBody body = camera1[ids.Item1];
                body.Id = GeneratedIdsMap[(ids.Item1, ids.Item2)];
                bestBodies.Add(body);
            }
            else
            {
                SimplifiedBody body = camera2[ids.Item2];
                body.Id = GeneratedIdsMap[(ids.Item1, ids.Item2)];
                bestBodies.Add(TransformBody(body));
            }
        }
        private void SelectByLearnedBodies(Dictionary<uint, SimplifiedBody> camera1, Dictionary<uint, SimplifiedBody> camera2, Tuple<uint, uint> ids, ref List<SimplifiedBody> bestBodies)
        {
            SimplifiedBody b1 = camera1[ids.Item1], b2 = camera2[ids.Item2];
            LearnedBody l1 = Camera1LearnedBodies[ids.Item1], l2 = Camera2LearnedBodies[ids.Item1];

            List<double> dist1 = new List<double>(), dist2 = new List<double>();
            foreach(var bones in l1.LearnedBones)
            {
                if (bones.Value > 0.0)
                    dist1.Add(Math.Abs(MathNet.Numerics.Distance.Euclidean(b1.Joints[bones.Key.ParentJoint].Item2.ToVector(), b1.Joints[bones.Key.ChildJoint].Item2.ToVector()) - bones.Value));
                if (l2.LearnedBones[bones.Key] > 0.0)
                    dist2.Add(Math.Abs(MathNet.Numerics.Distance.Euclidean(b2.Joints[bones.Key.ParentJoint].Item2.ToVector(), b2.Joints[bones.Key.ChildJoint].Item2.ToVector()) - l2.LearnedBones[bones.Key]));
            }
            var statistics1 = Statistics.MeanStandardDeviation(dist1);
            var statistics2 = Statistics.MeanStandardDeviation(dist2);
            if (statistics1.Item2 < statistics2.Item2)
            {
                SimplifiedBody body = camera1[ids.Item1];
                body.Id = GeneratedIdsMap[(ids.Item1, ids.Item2)];
                bestBodies.Add(body);
            }
            else
            {
                SimplifiedBody body = camera2[ids.Item2];
                body.Id = GeneratedIdsMap[(ids.Item1, ids.Item2)];
                bestBodies.Add(TransformBody(body));
            }
        }

        private List<SimplifiedBody> SelectBestBody(Dictionary<uint, SimplifiedBody> camera1, Dictionary<uint, SimplifiedBody> camera2)
        {
            List<SimplifiedBody> bestBodies = new List<SimplifiedBody>();
            foreach(var pair in CorrespondanceList)
            {
                if (camera1.ContainsKey(pair.Item1) && camera2.ContainsKey(pair.Item2))
                {
                    if(Camera1LearnedBodies.ContainsKey(pair.Item1) && Camera1LearnedBodies.ContainsKey(pair.Item2))
                        SelectByLearnedBodies(camera1, camera2, pair, ref bestBodies);
                    else
                        SelectByConfidence(camera1, camera2, pair, ref bestBodies);
                }
                else if (pair.Item1 == 0 || !camera1.ContainsKey(pair.Item1))
                {
                    if (!camera2.ContainsKey(pair.Item2))
                        continue;
                    Tuple<uint, uint> tuple;
                    var enumT = KeyOrValueExistInList(pair, out tuple);
                    switch (enumT)
                    {
                        case TupleState.KeyAlreadyInserted:
                        case TupleState.GoodToInsert:
                            throw new Exception("oups");
                        case TupleState.AlreadyExist:
                        case TupleState.RightJokerFound:
                        case TupleState.LeftJokerFound:
                            break;
                    }
                    SimplifiedBody simplifiedBody = camera2[pair.Item2];
                    simplifiedBody.Id = GeneratedIdsMap[(tuple.Item1, tuple.Item2)];
                    bestBodies.Add(simplifiedBody);
                }
                else if (pair.Item2 == 0 || !camera2.ContainsKey(pair.Item2))
                {
                    if (!camera1.ContainsKey(pair.Item1))
                        continue;
                    Tuple<uint, uint> tuple;
                    var enumT = KeyOrValueExistInList(pair, out tuple);
                    switch (enumT)
                    {
                        case TupleState.KeyAlreadyInserted:
                        case TupleState.GoodToInsert:
                            throw new Exception("oups");
                        case TupleState.AlreadyExist:
                        case TupleState.RightJokerFound:
                        case TupleState.LeftJokerFound:
                            break;
                    }
                    SimplifiedBody simplifiedBody = camera1[pair.Item1];
                    simplifiedBody.Id = GeneratedIdsMap[(tuple.Item1, tuple.Item2)];
                    bestBodies.Add(simplifiedBody);
                }
            }
            return bestBodies;
        } 
        
        private TupleState KeyOrValueExistInList(Tuple<uint, uint> tuple, out Tuple<uint, uint> value)
        {
            // zero is joker
            int caseCheck = 0;
            if(tuple.Item1 == 0)
                caseCheck = 1;
            if(tuple.Item2 == 0)
                caseCheck += 2;
            switch(caseCheck)
            {
                case 0:
                    if (CorrespondanceList.Contains(tuple))
                    {
                        value = tuple;
                        return TupleState.AlreadyExist;
                    }
                    foreach (var iterator in CorrespondanceList)
                    {
                        if (((iterator.Item2 == tuple.Item2 && iterator.Item1 != 0) || (iterator.Item1 == tuple.Item1 && iterator.Item2 !=0)))
                        {
                            value = iterator;
                            return TupleState.KeyAlreadyInserted;
                        }
                    }
                     break;
                case 1:
                    foreach (var iterator in CorrespondanceList)
                    {
                        if (iterator.Item2 == tuple.Item2)
                        {
                            value = iterator;
                            return TupleState.LeftJokerFound;
                        }
                    }
                    break;
                case 2:
                    foreach (var iterator in CorrespondanceList)
                    {
                        if (iterator.Item1 == tuple.Item1)
                        {
                            value = iterator;
                            return TupleState.RightJokerFound;
                        }
                    }
                    break;
                case 3:
                    throw new Exception("Critical bug");
            }
            value = tuple;
            return TupleState.GoodToInsert;
        }

        private double AccumulatedConfidence(SimplifiedBody body)
        {
            //might use coef for usefull joints.
            int accumulator = 0;
            foreach(var joint in body.Joints)
                accumulator+=(int)joint.Value.Item1;
            return accumulator;
        }

        private SimplifiedBody TransformBody(SimplifiedBody body)
        {
            if(Configuration.Camera2ToCamera1Transformation == null)
                return body;
            SimplifiedBody transformed = body.DeepClone();
            foreach (var joint in body.Joints)
                transformed.Joints[joint.Key] = new Tuple<JointConfidenceLevel, Vector3D>(joint.Value.Item1, Helpers.Helpers.CalculateTransform(joint.Value.Item2, Configuration.Camera2ToCamera1Transformation));
            return transformed;
        }
    }

    internal class TupleDoubleUintComparer : Comparer<Tuple<double, uint>>
    {
        public override int Compare(Tuple<double, uint> a, Tuple<double, uint> b)
        {
            if(a.Item1 == b.Item1)
                return 0;
            return a.Item1 > b.Item1 ? 1 : -1;
        }
    }
}
