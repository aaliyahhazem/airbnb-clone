import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { UserPreferencesService } from '../../../core/services/user-preferences/user-preferences.service';

@Component({
  selector: 'app-personalization-badge',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    <div class="personalization-badge" *ngIf="hasPreferences"
         [title]="'listings.personalizedResults' | translate">
      <i class="bi bi-stars"></i>
      <span>{{ 'listings.personalized' | translate }}</span>
      <div class="badge-tooltip">
        <p>{{ 'listings.personalizedTooltip' | translate }}</p>
        <div class="top-amenities" *ngIf="topAmenities.length > 0">
          <small>{{ 'listings.yourPreferences' | translate }}:</small>
          <div class="amenity-tags">
            <span class="amenity-tag" *ngFor="let amenity of topAmenities">
              {{ amenity }}
            </span>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .personalization-badge {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      padding: 6px 12px;
      border-radius: 20px;
      font-size: 0.85rem;
      font-weight: 500;
      cursor: help;
      position: relative;
      box-shadow: 0 2px 8px rgba(102, 126, 234, 0.3);
      transition: all 0.3s ease;
    }

    .personalization-badge:hover {
      transform: translateY(-2px);
      z-index: 5;
      box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
    }

    .personalization-badge i {
      font-size: 1rem;
      animation: sparkle 2s infinite;
    }

    @keyframes sparkle {
      0%, 100% { opacity: 1; }
      50% { opacity: 0.6; }
    }

    .badge-tooltip {
      position: absolute;
      top: calc(100% + 10px);
      left: 50%;
      z-index: 5;
      transform: translateX(-50%);
      background: white;
      color: #333;
      padding: 12px 16px;
      border-radius: 8px;
      box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
      min-width: 250px;
      opacity: 0;
      pointer-events: none;
      transition: opacity 0.3s ease;
      z-index: 5000;
    }

    .personalization-badge:hover .badge-tooltip {
      opacity: 1;
      z-index: 5;
      pointer-events: auto;
    }

    .badge-tooltip::before {
      content: '';
      position: absolute;
      top: -6px;
      left: 50%;
      transform: translateX(-50%);
      width: 0;
      height: 0;
      border-left: 6px solid transparent;
      border-right: 6px solid transparent;
      border-bottom: 6px solid white;
    }

    .badge-tooltip p {
      margin: 0 0 8px 0;
      font-size: 0.9rem;
      line-height: 1.4;
    }

    .top-amenities {
      margin-top: 8px;
      padding-top: 8px;
      border-top: 1px solid #eee;
    }

    .top-amenities small {
      display: block;
      margin-bottom: 6px;
      color: #666;
      font-weight: 500;
    }

    .amenity-tags {
      display: flex;
      flex-wrap: wrap;
      gap: 4px;
    }

    .amenity-tag {
      display: inline-block;
      background: #f0f0f0;
      color: #555;
      padding: 2px 8px;
      border-radius: 12px;
      font-size: 0.75rem;
    }

    @media (max-width: 768px) {
      .badge-tooltip {
        left: auto;
        right: 0;
        transform: none;
        min-width: 200px;
      }

      .badge-tooltip::before {
        left: auto;
        right: 20px;
        transform: none;
      }
    }
  `]
})
export class PersonalizationBadge implements OnInit {
  hasPreferences = false;
  topAmenities: string[] = [];

  constructor(private userPreferences: UserPreferencesService) {}

  ngOnInit(): void {
    const stats = this.userPreferences.getPreferenceStats();
    this.hasPreferences = stats.totalAmenities > 0 || stats.totalDestinations > 0;
    this.topAmenities = stats.topAmenities.slice(0, 3);
  }
}
