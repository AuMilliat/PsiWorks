using Microsoft.Psi;
using Microsoft.Psi.Components;

namespace Helpers
{
    public class PipeToMessage<T> : Subpipeline
    {

        /// <summary>
        /// </summary>
        private Connector<T> InConnector;

        /// <summary>
        /// </summary>
        public Receiver<T> In => InConnector.In;

        public delegate void Do(Message<T> message);
        private Do delegateDo;

        public PipeToMessage(Pipeline parent, Do toDo, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null)
          : base(parent, name, defaultDeliveryPolicy)
        {
            delegateDo = toDo;
            InConnector = CreateInputConnectorFrom<T>(parent, nameof(In));
            InConnector.Out.Do(Process);
        }

        private void Process(T data, Envelope envelope)
        {
            Message<T> message = new Message<T>(data, envelope.OriginatingTime,envelope.CreationTime, envelope.SourceId, envelope.SequenceId);
            delegateDo(message);
        }
    }
}
