import { Component } from '@angular/core';
import { ListingCard } from '../listing-card/listing-card';

@Component({
  selector: 'app-listings',
  imports: [ListingCard],
  templateUrl: './listings.html',
  styleUrl: './listings.css',
})
export class Listings {

}
