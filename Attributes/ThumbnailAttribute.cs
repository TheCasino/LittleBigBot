using System;

namespace LittleBigBot.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ThumbnailAttribute : Attribute
    {
        public ThumbnailAttribute(string imageUrl)
        {
            ImageUrl = imageUrl;
        }

        public string ImageUrl { get; }
    }
}