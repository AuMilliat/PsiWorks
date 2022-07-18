using System;
using System.Threading;
using Microsoft.Psi.Components;
using Microsoft.Psi;
using Microsoft.Psi.Imaging;
using MQTTnet;
using MQTTnet.Client;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Numerics;




namespace ORMonitoring
{
    class PozyxEnvironment
    {
        public PozyxTag[] tags;
        public PozyxAnchor[] anchors;
        public PozyxObject[] objects;
        public PozyxArea[] areas;

        public int idxTag = 0;
        public int idxAnchor = 0;
        public int idxObject = 0;
        public int idxArea = 0;

        public string receiverID;
            
        private float x_min = float.MaxValue;
        private float x_max = float.MinValue;
        private float y_min = float.MaxValue;
        private float y_max = float.MinValue;
        /*        private float z_min = float.MaxValue;
                private float z_max = float.MinValue;*/

        public int ImageXMax;
        public int ImageYMax;

        private float ImageXOffset;
        private float ImageYOffset;

        private int ImageAxeMax;

        public int AnchorSize = 15;
        public System.Drawing.Color AnchorColor = System.Drawing.Color.White;
        private float ScaleFactor;
        
        public int TagCircleSize = 10;
        public int TagDirectionLength = 20;

        public PozyxEnvironment(string receiverID, int ImageAxeMax = 500, int nbrTags=10, int nbrAnchors=10, int nbrObjects=10, int nbrAreas=10)
        {
            this.receiverID = receiverID;
            this.ImageAxeMax = ImageAxeMax;
            this.tags = new PozyxTag[nbrTags];
            this.anchors = new PozyxAnchor[nbrAnchors];
            this.objects = new PozyxObject[nbrObjects];
            this.areas = new PozyxArea[nbrAreas];

            PozyxTag receiver = new PozyxTag(receiverID, "Receiver", System.Drawing.Color.Black);
            this.addTag(receiver);
        }

        public void addTag(PozyxTag tag)
        {
            tags[idxTag] = tag;
            idxTag++;
        }
        public int getTagIdx(string id)
        {
            for (int i = 0; i < tags.Length; i++)
            {
                if (tags[i].id == id)
                {
                    return i;
                }
            }
            return -1;
        }
        public void addAnchor(PozyxAnchor anchor)
        {
            anchors[idxAnchor] = anchor;
            idxAnchor++;
        }
        public void addObject(PozyxObject pozObject)
        {
            objects[idxObject] = pozObject;
            idxObject++;
        }
        public void addArea(PozyxArea area)
        {
            areas[idxArea] = area;
            idxArea++;
        }

        public void computeAnchorSpace()
        {

            for (int k=0; k < idxAnchor; k++)
            {
                PozyxAnchor anchor = this.anchors[k];
                this.x_min = Math.Min(anchor.position.X, this.x_min);
                this.x_max = Math.Max(anchor.position.X, this.x_max);
                this.y_min = Math.Min(anchor.position.Y, this.y_min);
                this.y_max = Math.Max(anchor.position.Y, this.y_max);
                /*            this.z_min = Math.Min(anchor.position.Z, this.z_min);
            this.z_max = Math.Max(anchor.position.Z, this.z_max);*/
            }

            this.ImageXOffset = this.x_min;
            this.ImageYOffset = this.y_min;

            float ImageXSize = this.x_max - this.x_min;
            float ImageYSize = this.y_max - this.y_min;

            if (ImageXSize > ImageYSize)
            {
                this.ScaleFactor = this.ImageAxeMax / ImageXSize;
            }
            else
            {
                this.ScaleFactor = this.ImageAxeMax / ImageYSize;
            }
            this.ImageXMax = (int)(ImageXSize * this.ScaleFactor);
            this.ImageYMax = (int)(ImageYSize * this.ScaleFactor);
        }

        public Vector3 ComputePosition(Vector3 pos)
        {
            Vector3 newPosition = pos.DeepClone();
            newPosition.X = (pos.X - this.ImageXOffset) * this.ScaleFactor;
            newPosition.Y = (pos.Y - this.ImageYOffset) * this.ScaleFactor;
            return newPosition;
        }

        public Vector2 ComputePosition(Vector2 pos)
        {
            Vector2 newPosition = pos.DeepClone();
            newPosition.X = (pos.X - this.ImageXOffset) * this.ScaleFactor;
            newPosition.Y = (pos.Y - this.ImageYOffset) * this.ScaleFactor;
            return newPosition;
        }

        public Vector3 GetImagePosition(PozyxAnchor anchor)
        {
            Vector3 newPosition = this.ComputePosition(anchor.position);
            if (newPosition.X > this.ImageXMax / 2)
            {
                newPosition.X -= this.AnchorSize;
            }
            if (newPosition.Y > this.ImageYMax / 2)
            {
                newPosition.Y -= this.AnchorSize;
            }
            return newPosition;
        }

        public (Vector3, Vector3) GetImagePosition(PozyxTag tag)
        {
            Vector3 tagPosition = this.ComputePosition(tag.position);
            Vector3 tagOrientation = tag.orientation.DeepClone();
            tagOrientation.Z = tagOrientation.Z - tag.orientationInit.Z + (float)Math.PI / 2;
            tagOrientation.Z = (float)Math.PI - tagOrientation.Z;
            return (tagPosition, tagOrientation);
        }
    }
    
    class PozyxTag
    {
        public string id;
        public string person;
        public Vector3 position;
        public Vector3 orientation;
        public Vector3 orientationInit;
        public System.Drawing.Color color = System.Drawing.Color.Blue;

        public PozyxTag(string id, string person, System.Drawing.Color color)
        {
            this.id = id;
            this.person = person;
            this.color = color;
        }

        public void UpdateTag(Vector3 position, Vector3 orientation, System.Drawing.Color color)
        {
            this.position = position;
            this.orientation = orientation;
    }

        public void SetOrientationInit(Vector3 orientation)
        {
            this.orientationInit = orientation;
        }

    }
    
    class PozyxAnchor {
        public Vector3 position;
        public string id;
        public PozyxAnchor(string id, Vector3 position)
        {
            this.id = id;
            this.position = position;
        }
    }

    class PozyxObject
    {
        public string shape;
        public Vector2[] points;
        public System.Drawing.Color color;
        public string name;
        public PozyxObject(string name, Vector2[] points, string shape, System.Drawing.Color color)
        {
            this.name = name;
            this.points = points;
            this.shape = shape;
            this.color = color;
        }
    }

    class PozyxArea
    {
        public string shape;
        public Vector2[] points;
        public System.Drawing.Color color;
        public string name;
        public float drawWidth;
        public PozyxArea(string name, Vector2[] points, string shape, System.Drawing.Color color, float drawWith )
        {
            this.name = name;
            this.points = points;
            this.shape = shape;
            this.color = color;
            this.drawWidth = drawWith;
        }
    }

        internal class Pozyx : ISourceComponent, IDisposable
    {
        public Emitter<Shared<Image>> Video { get; private set; }
        public Emitter<PozyxEnvironment> RawData { get; private set; }

        private Thread captureThread = null;
        private bool shutdown = false;

        private IMqttClient mqttClient;
        private MqttFactory mqttFactory;
        private MqttClientOptions mqttClientOptions;

        private PozyxEnvironment environment;

        private int TimeWait = 100;

        private DateTime LastTime;


        private Shared<Image> imageBack = null;
        private Shared<Image> imageRoom = null;

        private bool ReadyToPublish;

        public Pozyx(Pipeline pipeline, PozyxEnvironment environment)
        {
            this.environment = environment;

            CreateImageBack();
            mqttFactory = new MqttFactory();
            mqttClient = mqttFactory.CreateMqttClient();
            mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer("localhost",1883).Build();
            this.Video = pipeline.CreateEmitter<Shared<Image>>(this, nameof(this.Video));
            this.RawData = pipeline.CreateEmitter<PozyxEnvironment>(this, nameof(this.RawData));
            this.LastTime = DateTime.UtcNow;
        }

        private void CreateImageBack()
        {
            this.imageBack = ImagePool.GetOrCreate(this.environment.ImageXMax, this.environment.ImageYMax, PixelFormat.RGB_24bpp);
            DrawAnchors();
            DrawObjects();
            this.imageRoom = ImagePool.GetOrCreate(this.environment.ImageXMax, this.environment.ImageYMax, PixelFormat.BGRA_32bpp);
            this.imageRoom.Resource.CopyFrom(this.imageBack.Resource.DeepClone());
        }
        
        public void DrawAnchors()
        {
            for (int k = 0; k < environment.idxAnchor; k++)
            {
                PozyxAnchor anchor = environment.anchors[k];
                Vector3 newPos = environment.GetImagePosition(anchor);
                this.imageBack.Resource.FillRectangle(new System.Drawing.Rectangle((int)newPos.X, (int)newPos.Y, environment.AnchorSize, environment.AnchorSize), environment.AnchorColor);
            }
        }

        public void DrawObjects()
        {
            for (int k = 0; k < environment.idxObject; k++)
            {
                PozyxObject pozyxObject = environment.objects[k];
                if (pozyxObject.shape == "rectangle")
                {
                    DrawObjectRectangle(pozyxObject);
                }
            }
        }

        public void DrawTags()
        {
            this.imageRoom.Resource.CopyFrom(this.imageBack.Resource.DeepClone());
            for (int k=0; k<environment.idxTag; k++)
            {
                PozyxTag tag = environment.tags[k];
                if (tag.id != environment.receiverID)
                {
                    (Vector3 tagPosition, Vector3 tagOrientation) = environment.GetImagePosition(tag);
                    this.imageRoom.Resource.FillCircle(new System.Drawing.Point((int) tagPosition.X, (int) tagPosition.Y), environment.TagCircleSize, tag.color);
                    this.imageRoom.Resource.DrawLine(new System.Drawing.Point((int)tagPosition.X, (int)tagPosition.Y), new System.Drawing.Point((int)tagPosition.X + (int)(Math.Cos(tagOrientation.Z) * this.environment.TagDirectionLength),
                        (int)tagPosition.Y + (int)(Math.Sin(tagOrientation.Z) * this.environment.TagDirectionLength)), tag.color, this.environment.TagDirectionLength / 4);
                }
            }
            this.imageRoom.Resource.CopyFrom(this.imageRoom.Resource.Flip(FlipMode.AlongHorizontalAxis));
        }
        
        public void DrawObjectRectangle(PozyxObject pozyxObject)
        {
            Vector2[] rectanglePos = pozyxObject.points;
            for (int k = 0; k < rectanglePos.Length; k++)
            {
                rectanglePos[k] = environment.ComputePosition(rectanglePos[k]);
            }
            
            Vector2 vect1 = (rectanglePos[1] + rectanglePos[0]) / 2;
            System.Drawing.Point point1 = new System.Drawing.Point((int)vect1.X, (int)vect1.Y);

            Vector2 vect2 = vect1 + (rectanglePos[2]-rectanglePos[1]);
            System.Drawing.Point point2 = new System.Drawing.Point((int)vect2.X, (int)vect2.Y);
            
            int length = (int) (rectanglePos[1] - rectanglePos[0]).Length();

            this.imageBack.Resource.DrawLine(point1, point2, pozyxObject.color, length);
        }
        
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            notifyCompletionTime(DateTime.MaxValue);
            this.captureThread = new Thread(new ThreadStart(this.CaptureThreadProc));
            this.captureThread.Start();
            PozyxInit();
        }

        async void PozyxInit()
        {
            var response = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

            Console.WriteLine("The MQTT client is connected.");
            mqttClient.ApplicationMessageReceivedAsync += ReceivedHandler;
            await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("tags").Build());
        }

        public async System.Threading.Tasks.Task ReceivedHandler(MqttApplicationMessageReceivedEventArgs msg)
        {
            var message = Encoding.UTF8.GetString(msg.ApplicationMessage.Payload);
            var messageData = JArray.Parse(message);
            var messageObj = JArray.Parse(messageData.ToString());

            foreach (var tagData in messageObj)
            {
                if (!(bool)tagData["success"]) continue;

                string tagId = (string)tagData["tagId"];

                Vector3 tagPosition = new Vector3((float)tagData["data"]["coordinates"]["x"], (float)tagData["data"]["coordinates"]["y"],
                    (float)tagData["data"]["coordinates"]["z"]);
                Vector3 tagOrientation = new Vector3((float)tagData["data"]["orientation"]["pitch"],
                    (float)tagData["data"]["orientation"]["roll"],
                    (float)tagData["data"]["orientation"]["yaw"]);
                
                if (this.environment.getTagIdx(tagId)==-1)
                {
                    PozyxTag newTag = new PozyxTag("unknown", tagId, System.Drawing.Color.Blue);
                    this.environment.addTag(newTag);
                    
                }
                
                int currentIdx = this.environment.getTagIdx(tagId);

                if (this.environment.tags[currentIdx].orientationInit == Vector3.Zero)
                {
                    this.environment.tags[currentIdx].orientationInit = tagOrientation;
                }

                this.environment.tags[currentIdx].position = tagPosition;
                this.environment.tags[currentIdx].orientation = tagOrientation;
            }
            this.ReadyToPublish = true;
        }
        
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            shutdown = true;
            TimeSpan waitTime = TimeSpan.FromSeconds(1);
            if (this.captureThread != null && this.captureThread.Join(waitTime) != true)
            {
#pragma warning disable SYSLIB0006 // Le type ou le membre est obsolète
                captureThread.Abort();
#pragma warning restore SYSLIB0006 // Le type ou le membre est obsolète
            }
            notifyCompleted();
            PozyxOver();

        }

        async void PozyxOver()
        {
            var mqttClientDisconnectOptions = mqttFactory.CreateClientDisconnectOptionsBuilder().Build();
            await mqttClient.DisconnectAsync(mqttClientDisconnectOptions, CancellationToken.None);
        }

        private void CaptureThreadProc()
        {
            
            while (!this.shutdown)
            {
                DateTime now = DateTime.UtcNow;
                if ((now - this.LastTime).TotalMilliseconds > this.TimeWait && this.ReadyToPublish)
                {
                    this.DrawTags();
                    this.Video.Post(this.imageRoom,now);
                    this.RawData.Post(this.environment, now);
                    this.ReadyToPublish = false;
                    this.LastTime = now;
                }
            }
        }

    
        public void Dispose()
        {

        }

    }

}
