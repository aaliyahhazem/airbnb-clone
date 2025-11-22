namespace DAL.Entities
{
    public class Amenity
    {
        public int Id { get; private set; }
        public string Word { get; private set; } = null!;

        // FK -> Listing (one-to-many)
        public int ListingId { get; private set; }
        public Listing Listing { get; private set; } = null!;

        private Amenity() { }

        // Factory
        public static Amenity Create(string word, Listing listing)
        {
            if (listing == null) throw new ArgumentNullException(nameof(listing));
            if (string.IsNullOrWhiteSpace(word)) throw new ArgumentException("Amenity cannot be empty.", nameof(word));

            var kw = new Amenity
            {
                Word = word.Trim(),
                Listing = listing
                // ListingId will be set by EF when saved (or you can set it to listing.Id if you prefer)
            };
            return kw;
        }

        internal void Update(string word)
        {
            Word = word?.Trim() ?? Word;
        }
    }
}
