namespace BusinessLogicLayer.Constants
{
    public static class MediaConstants
    {
        // Default images now in SharedMedia/defaults
        public static class Defaults
        {
            public const string DefaultAvatarPath = "/SharedMedia/defaults/default-avatar.svg";
            public const string DefaultCoursePath = "/SharedMedia/defaults/default-course.svg";
            public const string DefaultAchievementPath = "/SharedMedia/defaults/default-achievement.svg";
            public const string DefaultLogoPath = "/SharedMedia/defaults/default-logo.svg";
        }

        // Static assets now in SharedMedia
        public static class StaticAssets
        {
            public const string LogoPath = "/SharedMedia/logo/logowithoutbackground.png";
            public const string MainLogoPath = "/SharedMedia/logo/Main_Logo.jpg";
            public const string LoginBackgroundPath = "/SharedMedia/static/login-bg.jpg";
            public const string EmptyCoursesPath = "/SharedMedia/static/empty-courses.svg";
            public const string EmptyRecommendationsPath = "/SharedMedia/static/empty-recommendations.svg";
            public const string LoginIllustrationPath = "/SharedMedia/static/login-illustration.svg";
            public const string DefaultAvatarJpgPath = "/SharedMedia/static/default-avatar.jpg";
            public const string DefaultCoursePngPath = "/SharedMedia/static/default-course.png";
        }

        // SharedMedia paths for uploaded content
        public static class SharedMedia
        {
            public const string AvatarsPath = "/SharedMedia/avatars/";
            public const string CoursesPath = "/SharedMedia/courses/";
            public const string DocumentsPath = "/SharedMedia/documents/";
            public const string UploadsPath = "/SharedMedia/uploads/";
            public const string ImagesPath = "/SharedMedia/images/";
            public const string BannersPath = "/SharedMedia/banners/";
            public const string IconsPath = "/SharedMedia/icons/";
            public const string LogoPath = "/SharedMedia/logo/";
            public const string DefaultsPath = "/SharedMedia/defaults/";
            public const string StaticPath = "/SharedMedia/static/";
        }

        // Media categories for MediaPathService
        public static class Categories
        {
            public const string Avatars = "avatars";
            public const string Courses = "courses";
            public const string Documents = "documents";
            public const string Uploads = "uploads";
            public const string Images = "images";
            public const string Banners = "banners";
            public const string Icons = "icons";
            public const string Logo = "logo";
            public const string Defaults = "defaults";
            public const string Static = "static";
        }
    }
}