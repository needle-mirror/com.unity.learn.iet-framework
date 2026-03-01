using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor.Paragraphs
{
    /// <summary>
    /// A paragraph of a <see cref="TutorialPage"/> used to display an image or a video.
    /// </summary>
    /// <inheritdoc cref="ParagraphBase" />
    public class MediaParagraph: ParagraphBase
    {
        /// <summary>
        /// The media to display.
        /// </summary>
        public MediaContent Media;

        /// <inheritdoc />
        public override bool CanMask() => true;

        /// <inheritdoc />
        public override VisualElement GetDisplayRoot()
        {
            //TODO : manage to define default uxml without having to load it every time
            TemplateContainer root = UIUtils.LoadUXML("Paragraphs/Media").CloneTree();

            if(Media.ContentType == MediaContent.MediaContentType.Image)
            {
                // Image
                if (Media.IsValid())
                {
                    UIUtils.Show("TutorialMediaContainer", root);
                    UIUtils.Hide("VideoPlayerRoot", root);

                    root.Q("TutorialMedia").style.backgroundImage = Media.Image;

                    // Popout button
                    VisualElement popout = root.Q<VisualElement>("PopoutButton");
                    popout.AddManipulator(new Clickable(() =>
                    {
                        MediaPopoutWindow.Popout(root);
                    }));
                }
                else
                {
                    UIUtils.Hide("TutorialMediaContainer", root);
                }
            }
            else
            {
                // Video
                if (Media.IsValid())
                {
                    UIUtils.Show("TutorialMediaContainer", root);
                    UIUtils.Hide("TutorialMedia", root);

                    VideoPlayerElement vidPlayer = root.Q<VideoPlayerElement>();

                    if (Media.ContentType == MediaContent.MediaContentType.VideoClip)
                    {
                        vidPlayer.SetVideoClip(Media.VideoClip, Media.AutoStart);
                    }
                    else if (Media.ContentType == MediaContent.MediaContentType.VideoUrl)
                    {
                        vidPlayer.SetVideoUrl(Media.Url, Media.AutoStart);
                    }

                    vidPlayer.SetLooping(Media.Loop);
                }
                else
                {
                    UIUtils.Hide("TutorialMediaContainer", root);
                }
            }

            return root;
        }
    }
}
