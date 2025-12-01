import { Routes } from '@angular/router';
import { Home } from './features/home-page/home/home';
import { ListingsList } from './features/listings/list/listing-list';
import { ListingsCreateEdit } from './features/listings/create-edit/listings-create-edit';
import { listingExistsGuard } from './core/services/listings/listing-exists.guard';
import { AuthGuard } from './core/guards/auth.guard';
import { ListingsDetail } from './features/listings/detail/listings-detail';
import { Login } from './features/auth/login';
import { Register } from './features/auth/register';
import { Dashboard } from './features/admin/dashboard';
import { ChatWindow } from './features/message/chat-window';
import { NotificationWindow } from './features/notification/notification-window';
import { UserListingsComponent } from './features/listings/user-listings/user-listings';
import { AdminListingsComponent } from './features/listings/admin-listings/admin-listings';
import { MapComponent } from './features/Map/map/map';
import { Listings } from './features/listings-page/listings/listings';
import { AdminDashboard } from './features/admin/admin-dashboard/admin-dashboard';
import { AboutComponent } from './features/about/about';
import { ContactComponent } from './features/contact/contact';

import { FavoritePage } from './features/favorites/favorite-page/favorite-page';
import { OnboardingWalkthrough } from './features/onboarding/onboarding-walkthrough';

export const routes: Routes = [
  { path: 'home', component: Home },
  { path: 'map', component: MapComponent },
  { path: 'about', component: AboutComponent },
  { path: 'contact', component: ContactComponent },

  { path: '', pathMatch: 'full', redirectTo: 'home' },
  { path: 'auth/login', component: Login },
  { path: 'auth/register', component: Register },
  { path: 'onboarding', component: OnboardingWalkthrough, canActivate: [AuthGuard] },
  //{ path: 'listings/:id/edit', component: ListingsCreateEdit, canActivate: [AuthGuard] },
  { path: 'listings/:id', component: ListingsDetail },
  { path: 'listings', component: Listings},
  // { path: '**', redirectTo: 'home' },
  {
    path: 'host',
    canActivate: [AuthGuard],
    children: [
      { path: '', component: ListingsList },
      { path: 'create', component: ListingsCreateEdit, canActivate: [AuthGuard] },

      // listing detail/edit routes
      { path: ':id/edit', component: ListingsCreateEdit},
      { path: ':id', component: ListingsDetail,  },
    ],
  },

  // user / admin lists
  { path: 'my-listings', component: UserListingsComponent },
  { path: 'admin/listings', component: AdminListingsComponent },

  // other app routes
  { path: 'admin', component: AdminDashboard, canActivate: [AuthGuard] },
  { path: 'messages', component: ChatWindow, canActivate: [AuthGuard] },
  { path: 'notifications', component: NotificationWindow, canActivate: [AuthGuard] },
  //Favaorites
  { path: 'favorites', component: FavoritePage, canActivate: [AuthGuard] },

  //Bookings
   {
    path: 'booking',
    canActivate: [AuthGuard],
    children: [
      {
        path: 'create/:id',
        loadComponent: () => import('./features/booking/create-booking/create-booking').then(m => m.CreateBooking)
      },
      {
        path: 'my-bookings',
        loadComponent: () => import('./features/booking/my-bookings/my-bookings').then(m => m.MyBookings)
      },
      {
        path: 'host-bookings',
        loadComponent: () => import('./features/booking/host-bookings/host-bookings').then(m => m.HostBookings)
      }
    ]
  },
  {
  path: 'booking/payment/:bookingId',
  loadComponent: () =>
    import('./features/payment/stripe-payment/stripe-payment')
      .then(m => m.StripePayment)
},

  // { path: '**', redirectTo: 'home' },
];

