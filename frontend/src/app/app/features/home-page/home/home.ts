import { Component, HostListener, ElementRef, AfterViewInit } from '@angular/core';
import { HeroCard } from "../hero-card/hero-card";
import { HomeListingCard } from "../home-listing-card/home-listing-card";
import { StackedCards } from "../stacked-cards/stacked-cards";

@Component({
  selector: 'app-home',
  imports: [HeroCard, HomeListingCard, StackedCards],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home {

}
