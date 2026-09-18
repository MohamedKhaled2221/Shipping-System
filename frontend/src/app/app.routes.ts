import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { UserRole } from './core/models/enums';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/home/home.component').then((m) => m.HomeComponent),
  },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register/register.component').then((m) => m.RegisterComponent),
  },
  {
    path: 'products',
    loadComponent: () => import('./features/products/product-catalog.component').then((m) => m.ProductCatalogComponent),
  },
  {
    path: 'track',
    loadComponent: () => import('./features/tracking/tracking-lookup.component').then((m) => m.TrackingLookupComponent),
  },

  // Customer
  {
    path: 'checkout',
    canActivate: [authGuard, roleGuard],
    data: { roles: [UserRole.Customer] },
    loadComponent: () => import('./features/orders/checkout/checkout.component').then((m) => m.CheckoutComponent),
  },
  {
    path: 'orders',
    canActivate: [authGuard, roleGuard],
    data: { roles: [UserRole.Customer] },
    loadComponent: () => import('./features/orders/order-list/order-list.component').then((m) => m.OrderListComponent),
  },
  {
    path: 'orders/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/orders/order-detail/order-detail.component').then((m) => m.OrderDetailComponent),
  },

  // Admin
  {
    path: 'admin/shipments',
    canActivate: [authGuard, roleGuard],
    data: { roles: [UserRole.Admin] },
    loadComponent: () =>
      import('./features/admin/shipments/admin-shipment-list.component').then((m) => m.AdminShipmentListComponent),
  },
  {
    path: 'admin/shipments/:id',
    canActivate: [authGuard, roleGuard],
    data: { roles: [UserRole.Admin, UserRole.DeliveryAgent] },
    loadComponent: () =>
      import('./features/admin/shipments/shipment-detail/shipment-detail.component').then(
        (m) => m.ShipmentDetailComponent
      ),
  },
  {
    path: 'admin/delivery-agents',
    canActivate: [authGuard, roleGuard],
    data: { roles: [UserRole.Admin] },
    loadComponent: () =>
      import('./features/admin/delivery-agents/admin-delivery-agents.component').then(
        (m) => m.AdminDeliveryAgentsComponent
      ),
  },

  // Delivery agent
  {
    path: 'agent/shipments',
    canActivate: [authGuard, roleGuard],
    data: { roles: [UserRole.DeliveryAgent] },
    loadComponent: () =>
      import('./features/agent/my-shipments/agent-shipment-list.component').then((m) => m.AgentShipmentListComponent),
  },
  {
    path: 'agent/shipments/:id',
    canActivate: [authGuard, roleGuard],
    data: { roles: [UserRole.Admin, UserRole.DeliveryAgent] },
    loadComponent: () =>
      import('./features/admin/shipments/shipment-detail/shipment-detail.component').then(
        (m) => m.ShipmentDetailComponent
      ),
  },
  {
    path: 'agent/profile',
    canActivate: [authGuard, roleGuard],
    data: { roles: [UserRole.DeliveryAgent] },
    loadComponent: () => import('./features/agent/profile/agent-profile.component').then((m) => m.AgentProfileComponent),
  },

  { path: '**', redirectTo: '' },
];
