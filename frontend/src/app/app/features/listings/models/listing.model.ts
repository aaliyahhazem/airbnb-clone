export interface Listing {
  id: number;
  title: string;
  description: string;
  price: number;
  location: string;
  rating: number;
  dateAvailable: string;
  imageUrl?: string;
}
