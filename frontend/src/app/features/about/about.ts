import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-about',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  templateUrl: './about.html',
  styleUrl: './about.css'
})
export class AboutComponent {
  stats = [
    { key: 'properties', value: '10,000+' },
    { key: 'users', value: '50,000+' },
    { key: 'cities', value: '100+' },
    { key: 'bookings', value: '1M+' }
  ];

  features = [
    {
      icon: '🔍',
      titleKey: 'about.features.smartSearch.title',
      descriptionKey: 'about.features.smartSearch.description'
    },
    {
      icon: '🛡️',
      titleKey: 'about.features.secureBooking.title',
      descriptionKey: 'about.features.secureBooking.description'
    },
    {
      icon: '⭐',
      titleKey: 'about.features.verifiedHosts.title',
      descriptionKey: 'about.features.verifiedHosts.description'
    },
    {
      icon: '💬',
      titleKey: 'about.features.support247.title',
      descriptionKey: 'about.features.support247.description'
    }
  ];

  team = [
    {
      name: 'Abdelkarim Refaey',
      titleKey: 'about.team.ceo.title',
      image: '/me.png'
    },
    {
      name: 'Abdelkarim Refaey',
      titleKey: 'about.team.cto.title',
      image: '/me.png'
    },
    {
      name: 'Abdelkarim Refaey',
      titleKey: 'about.team.designer.title',
      image: '/me.png'
    }
  ];
}
