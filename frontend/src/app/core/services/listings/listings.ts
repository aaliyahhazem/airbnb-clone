// // src/app/features/listings/services/listings.service.ts
// import { Injectable, PLATFORM_ID, computed, effect, inject, signal } from '@angular/core';
// import {  isPlatformBrowser } from '@angular/common';
// // If your model is under "models":
// import { Listing } from '../../models/listing.model';
// // If your folder name is "interface", use this instead:
// // import { Listing } from '../interface/listing.model';

// const STORAGE_KEY = 'airpnp:listings';

// /**
//  * ListingsService
//  * - Mock data powered by Angular Signals
//  * - Persists to localStorage (browser only; SSR safe)
//  * - CRUD + search
//  *
//  * Swap to real API later by replacing create/update/remove/refresh methods.
//  */
// @Injectable({ providedIn: 'root' })
// export class ListingsService {
//   private platformId = inject(PLATFORM_ID);
//   private readonly isBrowser = isPlatformBrowser(this.platformId);

//   // ---- State
//   private readonly _listings = signal<Listing[]>(this.loadInitial());
//   private readonly _nextId = signal<number>(this.computeNextId(this._listings()));

//   // ---- Public readonly signal
//   readonly listings = computed(() => this._listings());

//   constructor() {
//     // Persist only when running in the browser (SSR safe)
//     if (this.isBrowser) {
//       effect(() => {
//         try {
//           const data = JSON.stringify(this._listings());
//           localStorage.setItem(STORAGE_KEY, data);
//         } catch {
//           // ignore storage errors
//         }
//       });
//     }
//   }

//   // ---------- Queries ----------
//   getAll(): Listing[] {
//     return this._listings();
//   }

//   getById(id: number): Listing | undefined {
//     return this._listings().find(l => l.id === id);
//   }

//   search(term: string): Listing[] {
//     const t = term.trim().toLowerCase();
//     if (!t) return this._listings();
//     return this._listings().filter(l =>
//       (l.title ?? '').toLowerCase().includes(t) ||
//       (l.location ?? '').toLowerCase().includes(t) ||
//       (l.description ?? '').toLowerCase().includes(t)
//     );
//   }

//   // ---------- Commands (CRUD) ----------
//   create(payload: Omit<Listing, 'id'>): Listing {
//     const listing: Listing = { ...payload, id: this._nextId() };
//     this._listings.update(list => [listing, ...list]);
//     this._nextId.update(id => id + 1);
//     return listing;
//   }

//   update(id: number, changes: Partial<Listing>): Listing | undefined {
//     let updatedItem: Listing | undefined;
//     this._listings.update(list =>
//       list.map(item => {
//         if (item.id !== id) return item;
//         updatedItem = { ...item, ...changes, id };
//         return updatedItem!;
//       })
//     );
//     return updatedItem;
//   }

//   remove(id: number): boolean {
//     const exists = this._listings().some(l => l.id === id);
//     if (!exists) return false;
//     this._listings.update(list => list.filter(l => l.id !== id));
//     return true;
//   }

//   // ---------- Helpers ----------
//   private computeNextId(list: Listing[]): number {
//     return list.length ? Math.max(...list.map(l => l.id ?? 0)) + 1 : 1;
//   }

//   private loadInitial(): Listing[] {
//     // Try reading from localStorage only in the browser
//     if (this.isBrowser) {
//       try {
//         const raw = localStorage.getItem(STORAGE_KEY);
//         if (raw) return JSON.parse(raw) as Listing[];
//       } catch {
//         // ignore JSON/storage errors and fall back to seed
//       }
//     }

//     // Seed data (also used during SSR so server render doesn't break)
//     return [
//       {
//         id: 1,
//         title: 'Cozy Apartment',
//         description: 'City view, 2 beds, near metro.',
//         pricePerNight: 120,
//         location: 'Cairo',
//         latitude: 30.0444,
//         longitude: 31.2357,
//         maxGuests: 4,
//         isApproved: true,
//         isReviewed: false,
//         reviewCount: 0,
//         createdAt: '2025-11-01',
//         userId: 'user-1'
//       },
//       {
//         id: 2,
//         title: 'Beach House',
//         description: 'Sea view with private pool.',
//         pricePerNight: 250,
//         location: 'Alexandria',
//         latitude: 31.2001,
//         longitude: 29.9187,
//         maxGuests: 6,
//         isApproved: true,
//         isReviewed: false,
//         reviewCount: 0,
//         createdAt: '2025-11-05',
//         userId: 'user-2'
//       }
//     ];
//   }
// }
