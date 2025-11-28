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
import { BookingComponent } from './features/booking/booking';
import { PaymentComponent } from './features/payment/payment';
import { ChatWindow } from './features/message/chat-window';
import { NotificationWindow } from './features/notification/notification-window';
import { UserListingsComponent } from './features/listings/user-listings/user-listings';
import { AdminListingsComponent } from './features/listings/admin-listings/admin-listings';
import { MapComponent } from './features/Map/map/map';
import { Listings } from './features/listings-page/listings/listings';
import { AdminDashboard } from './features/admin/admin-dashboard/admin-dashboard';

import { FavoritePage } from './features/favorites/favorite-page/favorite-page'; 
export const routes: Routes = [
  { path: 'home', component: Home },
  { path: 'map', component: MapComponent },

  { path: '', pathMatch: 'full', redirectTo: 'home' },
  { path: 'auth/login', component: Login },
  { path: 'auth/register', component: Register },
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
  { path: 'booking', component: BookingComponent, canActivate: [AuthGuard] },
  { path: 'payment/:id', component: PaymentComponent, canActivate: [AuthGuard] },
  { path: 'messages', component: ChatWindow, canActivate: [AuthGuard] },
  { path: 'notifications', component: NotificationWindow, canActivate: [AuthGuard] },
  //Favaorites 
  { path: 'favorites', component: FavoritePage, canActivate: [AuthGuard] },
  // Optional:
  // { path: '**', redirectTo: 'home' },
];

