using System.Windows.Media.Imaging;
using Microsoft.Psi.Components;
using Image = Microsoft.Psi.Imaging.Image;
using Helpers;
using Microsoft.Psi;

namespace Visualizer
{
    public abstract class StreamVisualizer : BasicVisualizer
    {
        protected Connector<Shared<Image>> InColorImageConnector;

        public Receiver<Shared<Image>> InColorImage => InColorImageConnector.In;

        public StreamVisualizer(Pipeline pipeline) : base(pipeline)
        {
            InColorImageConnector = CreateInputConnectorFrom<Shared<Image>>(pipeline, nameof(InColorImage));
        }
    }
}
