import { Routes } from '@angular/router';
import { LoginComponent } from './features/login/login.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { CreateUserComponent } from './features/admin/create-user/create-user.component';
import { ShellComponent } from './core/layout/shell.component';
import { authGuard } from './core/guards/auth.guard';
import { adminGuard } from './core/guards/admin.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', component: DashboardComponent },
      { path: 'admin/users/new', component: CreateUserComponent, canActivate: [adminGuard] },
    ],
  },
  { path: '**', redirectTo: 'login' },
];
