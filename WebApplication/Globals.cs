namespace WebTestApplication
{
    public static class Globals
    {
        public const int PDF_PPI = 72;
        public const int SCREEN_PPI = 96;

        public const int PDF_MAX_HEIGHT_DPI = 14 * PDF_PPI;
        public const int PDF_MAX_WIDTH_DPI = 14 * PDF_PPI;    // could be 8.5, but 14 takes into account landscape documents
        public const int MIN_PDF_SIGNATURE_AREA_WIDTH = 1 * PDF_PPI; // 1" inch
        public const int MIN_PDF_SIGNATURE_AREA_HEIGHT = (int)(PDF_PPI / 2);
        public const int MAX_PDF_SIGNATURE_AREA_WIDTH = 5 * PDF_PPI;
        public const int MAX_PDF_SIGNATURE_AREA_HEIGHT = 5 * PDF_PPI;

        public const string LOCAL_SIGNATURE_NAME = "LocalSignature";
    }
}
