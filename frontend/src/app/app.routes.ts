import { Routes } from '@angular/router';
import { ListingsList } from './app/features/listings/list/listing-list';
import { ListingsCreateEdit } from './app/features/listings/create-edit/listings-create-edit';
import { listingExistsGuard } from './app/features/listings/services/listing-exists.guard';
import { ListingsDetail } from './app/features/listings/detail/listings-detail';


export const routes: Routes = [
  {
    path: 'listings',
    children: [
      { path: '', component: ListingsList },
      { path: 'create', component: ListingsCreateEdit },
      { path: 'edit/:id', component: ListingsCreateEdit, canActivate: [listingExistsGuard] },
      { path: 'detail/:id', component: ListingsDetail, canActivate: [listingExistsGuard] },
    ],
  },
  { path: '', pathMatch: 'full', redirectTo: 'listings' },
  { path: '**', redirectTo: 'listings' },
];
