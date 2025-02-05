using UnityEngine;
using UnityEngine.Video;

namespace Unity.Tutorials.Core.Editor
{
    /// <summary>
    /// Container class for a type of media used in a TutorialPage. Can be either an image, or a video (clip or url)
    /// </summary>
    [System.Serializable]
    public class MediaContent
    {
        /// <summary>
        /// The type of Media this define
        /// </summary>
        public enum MediaContentType
        {
            /// <summary>
            /// An Image
            /// </summary>
            Image,
            /// <summary>
            /// A Video from a file clip
            /// </summary>
            VideoClip,
            /// <summary>
            /// A video from a Url
            /// </summary>
            VideoUrl
        }

        /// <summary>
        /// Which type of source this content media uses
        /// </summary>
        public MediaContentType ContentType
        {
            get => m_ContentType;
            set => m_ContentType = value;
        }
        [SerializeField]
        private MediaContentType m_ContentType;

        /// <summary>
        /// The Texture2D used as Image if the Content Type is set to Image
        /// </summary>
        public Texture2D Image
        {
            get => m_Image;
            set => m_Image = value;
        }
        [SerializeField]
        private Texture2D m_Image;

        /// <summary>
        /// The Clip used if the source type is set to VideoClip
        /// </summary>
        public VideoClip VideoClip
        {
            get => m_VideoClip;
            set => m_VideoClip = value;
        }
        [SerializeField]
        private VideoClip m_VideoClip;

        /// <summary>
        /// The URL to the video if the source type is set to VideoURL
        /// </summary>
        public string Url
        {
            get => m_Url;
            set => m_Url = value;
        }
        [SerializeField]
        private string m_Url;

        /// <summary>
        /// If the Content type is video, does it auto start on load, or require the user to press play to start
        /// </summary>
        public bool AutoStart
        {
            get => m_AutoStart;
            set => m_AutoStart = value;
        }
        [SerializeField]
        private bool m_AutoStart = true;

        /// <summary>
        /// If the content is video, does it loop when it reaches the end.
        /// </summary>
        public bool Loop
        {
            get => m_Loop;
            set => m_Loop = value;
        }
        [SerializeField]
        private bool m_Loop = true;


        /// <summary>
        /// A Media Content is considered valid if the associated media to its set type is not null (e.g. an Image Media
        /// Content Image properties is not null or a VideoUrl Media url is not null or empty
        /// </summary>
        /// <returns>True if contains the right media, false otherwise</returns>
        public bool IsValid()
        {
            return (m_ContentType == MediaContentType.Image && m_Image != null) ||
                   (m_ContentType == MediaContentType.VideoClip && m_VideoClip != null) ||
                   (m_ContentType == MediaContentType.VideoUrl && !string.IsNullOrEmpty(m_Url));
        }
    }
}
