// Language switcher component for multilingual support
import { Component, inject, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LanguageService, SupportedLanguage } from '../../../core/services/language.service';

interface LanguageOption {
  code: SupportedLanguage;
  name: string;
  nativeName: string;
  flag: string;
}

@Component({
  selector: 'app-language-switcher',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="language-switcher">
      <button
        class="language-button"
        (click)="toggleDropdown(); $event.stopPropagation()"
        [attr.aria-label]="'Switch language'"
        type="button"
      >
        <span class="flag">{{ getCurrentLanguage().flag }}</span>
        <span class="language-code">{{ getCurrentLanguage().code.toUpperCase() }}</span>
        <svg class="dropdown-icon" width="12" height="12" viewBox="0 0 12 12" fill="none">
          <path d="M2.5 4.5L6 8L9.5 4.5" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>
      </button>

      <div class="dropdown-menu-custom" *ngIf="isDropdownOpen">
        <button
          *ngFor="let lang of languages"
          class="language-option"
          [class.active]="currentLanguage === lang.code"
          (click)="selectLanguage(lang.code); $event.stopPropagation()"
          type="button"
        >
          <span class="flag">{{ lang.flag }}</span>
          <div class="language-info">
            <span class="name">{{ lang.name }}</span>
            <span class="native-name">{{ lang.nativeName }}</span>
          </div>
          <svg
            *ngIf="currentLanguage === lang.code"
            class="check-icon"
            width="16"
            height="16"
            viewBox="0 0 16 16"
            fill="none"
          >
            <path d="M13.5 4L6 11.5L2.5 8" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
        </button>
      </div>
    </div>
  `,
  styles: [`
    .language-switcher {
      position: relative;
      display: inline-block;
    }

    .language-button {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 10px 10px;
      background: linear-gradient(135deg, #DC143C 0%, #B71C1C 100%);
      border: none;
      border-radius: 8px;
      color: white;
      cursor: pointer;
      font-size: 14px;
      font-weight: 700;
      transition: all 0.3s ease;
      min-width: 90px;
      justify-content: center;
      white-space: nowrap;
      overflow: hidden;
      box-shadow: 0 2px 8px rgba(220, 20, 60, 0.3);
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .language-button:hover {
      background: linear-gradient(135deg, #B71C1C 0%, #8B0000 100%);
      transform: translateY(-2px);
      box-shadow: 0 4px 16px rgba(220, 20, 60, 0.5);
    }

    .language-button:active {
      transform: translateY(0px);
      box-shadow: 0 2px 8px rgba(220, 20, 60, 0.3);
    }

    .flag {
      font-size: 20px;
      line-height: 1;
      flex-shrink: 0;
      filter: drop-shadow(0 1px 2px rgba(0, 0, 0, 0.2));
    }

    .language-code {
      font-size: 13px;
      flex-shrink: 0;
      font-weight: 700;
    }

    .dropdown-icon {
      transition: transform 0.3s ease;
      filter: drop-shadow(0 1px 2px rgba(0, 0, 0, 0.1));
    }

    .language-button:hover .dropdown-icon {
      transform: translateY(2px);
    }

    .dropdown-menu-custom {
      position: absolute;
      top: calc(100% + 12px);
      right: 0;
      background: white;
      border-radius: 12px;
      box-shadow: 0 8px 32px rgba(0, 0, 0, 0.2);
      border: 2px solid #DC143C;
      min-width: 220px;
      max-width: 220px;
      overflow: hidden;
      z-index: 9999;
      animation: fadeIn 0.2s ease-out;
    }

    @keyframes fadeIn {
      from {
        opacity: 0;
        transform: translateY(-8px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .language-option {
      display: flex;
      align-items: center;
      gap: 12px;
      width: 100%;
      padding: 14px 18px;
      background: white;
      border: none;
      border-left: 3px solid transparent;
      cursor: pointer;
      transition: all 0.2s ease;
      text-align: left;
      color: #222;
    }

    .language-option:hover {
      background: rgba(220, 20, 60, 0.05);
      border-left-color: #DC143C;
      transform: translateX(4px);
    }

    .language-option.active {
      background: rgba(220, 20, 60, 0.1);
      border-left-color: #DC143C;
      font-weight: 600;
    }

    .language-info {
      display: flex;
      flex-direction: column;
      flex: 1;
      min-width: 0;
      overflow: hidden;
    }

    .name {
      font-size: 14px;
      font-weight: 500;
      color: #222;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .native-name {
      font-size: 12px;
      color: #717171;
      margin-top: 2px;
      font-family: 'Cairo', sans-serif;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .check-icon {
      color: #DC143C;
      filter: drop-shadow(0 1px 2px rgba(220, 20, 60, 0.3));
    }

    /* RTL Support */
    [dir="rtl"] .dropdown-menu-custom {
      right: auto;
      left: 0;
    }

    [dir="rtl"] .language-option {
      text-align: right;
    }

    [dir="rtl"] .native-name,
    [dir="rtl"] .name {
      font-family: 'Cairo', sans-serif;
    }

    /* Dark text for visibility */
    .dropdown-menu-custom {
      color: #222;
    }
  `]
})
export class LanguageSwitcherComponent {
  private languageService = inject(LanguageService);

  isDropdownOpen = false;
  currentLanguage: SupportedLanguage = 'en';

  languages: LanguageOption[] = [
    {
      code: 'en',
      name: 'English',
      nativeName: 'English',
      flag: '🇺🇸'
    },
    {
      code: 'ar',
      name: 'Arabic',
      nativeName: 'العربية',
      flag: '🇸🇦'
    }
  ];

  constructor() {
    this.currentLanguage = this.languageService.getCurrentLanguage();

    // Subscribe to language changes
    this.languageService.currentLanguage$.subscribe(lang => {
      this.currentLanguage = lang;
    });
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    // Close dropdown when clicking outside
    this.isDropdownOpen = false;
  }

  toggleDropdown(): void {
    this.isDropdownOpen = !this.isDropdownOpen;
  }

  closeDropdown(): void {
    this.isDropdownOpen = false;
  }

  selectLanguage(lang: SupportedLanguage): void {
    this.languageService.switchLanguage(lang);
    this.closeDropdown();
  }

  getCurrentLanguage(): LanguageOption {
    return this.languages.find(l => l.code === this.currentLanguage) || this.languages[0];
  }
}
