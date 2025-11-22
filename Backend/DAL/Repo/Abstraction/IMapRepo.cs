using Microsoft.EntityFrameworkCore.Metadata.Internal;


namespace DAL.Repo.Abstraction
{

        public interface IMapRepo
        {
            Task<IEnumerable<Listing>> GetPropertiesInBoundsAsync(
                double northEastLat,
                double northEastLng,
                double southWestLat,
                double southWestLng);

            Task<Listing?> GetPropertyWithLocationAsync(int id);
        }
    
}
