import { Routes } from '@angular/router';
import { Home } from './app/features/home-page/home/home';
import { ListingsList } from './app/features/listings/list/listing-list';
import { ListingsCreateEdit } from './app/features/listings/create-edit/listings-create-edit';
import { listingExistsGuard } from './app/core/services/listings/listing-exists.guard';
import { AuthGuard } from './app/core/guards/auth.guard';
import { ListingsDetail } from './app/features/listings/detail/listings-detail';
import { Login } from './app/features/auth/login';
import { Register } from './app/features/auth/register';
import { Dashboard } from './app/features/admin/dashboard';
import { BookingComponent } from './app/features/booking/booking';
import { PaymentComponent } from './app/features/payment/payment';
import { ChatWindow } from './app/features/message/chat-window';
import { UserListingsComponent } from './app/features/listings/user-listings/user-listings';
import { AdminListingsComponent } from './app/features/listings/admin-listings/admin-listings';
import { MapComponent } from './app/features/Map/map/map';
import { Listings } from './app/features/listings-page/listings/listings';

export const routes: Routes = [
  { path: 'home', component: Home },
  { path: 'map', component: MapComponent },

  { path: '', pathMatch: 'full', redirectTo: 'home' },
  { path: 'auth/login', component: Login },
  { path: 'auth/register', component: Register },
  { path: 'listings', component: Listings},
  // { path: '**', redirectTo: 'home' },
  {
    path: 'host',
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
  { path: 'admin', component: Dashboard, canActivate: [AuthGuard] },
  { path: 'booking', component: BookingComponent, canActivate: [AuthGuard] },
  { path: 'payment/:id', component: PaymentComponent, canActivate: [AuthGuard] },
  { path: 'messages', component: ChatWindow, canActivate: [AuthGuard] },
  // Optional:
  // { path: '**', redirectTo: 'home' },
];

