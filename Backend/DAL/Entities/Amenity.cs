namespace DAL.Entities
{
    public class Amenity
    {
        public int Id { get; private set; }
        public string Name { get; private set; } = null!;

        // Relationships
        public ICollection<Listing> Listings { get; private set; } = new List<Listing>();

        private Amenity() { }

        //CreateImage a new amenity
        public static Amenity Create(string name)
        {
            return new Amenity
            {
                Name = name
            };
        }

        // Update existing amenity
        internal void Update(string name)
        {
            Name = name;
        }
    }
}