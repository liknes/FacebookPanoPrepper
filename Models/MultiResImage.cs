namespace FacebookPanoPrepper.Models
{
    public class MultiResImage
    {
        public string BasePath { get; set; }
        public int Levels { get; set; }
        public int TileSize { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}