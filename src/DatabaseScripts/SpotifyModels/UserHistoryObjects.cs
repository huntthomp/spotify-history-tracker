namespace Spotify.Models
{
    public class Item
    {
        public required Track track { get; set; }
        public required DateTimeOffset played_at { get; set; }
        //TODO: possibly track play context. playlist, recommended etc 
        //public required Context context { get; set; }
    }
    public class Cursors
    {
        public required long after { get; set; }
        public required long before { get; set; }
    }
    public class RecentlyPlayedResponse
    {
        public required List<Item> items { get; set; }
        public Cursors? cursors { get; set; }
    }
}