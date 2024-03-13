import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LoginComponent } from './login/login.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { authGuardFunction } from './services/auth-guard';
import { CreateUserComponent } from './create-user/create-user.component';
import { noAuthGuardFunction } from './services/no-auth-guard';
import { ManageAlgorithmComponent } from './manage-algorithm/manage-algorithm.component';

const routes: Routes = [
    {
        path: "",
        redirectTo: "dashboard",
        pathMatch: "full"
    },
    {
        path: "login",
        component: LoginComponent,
        canActivate: [noAuthGuardFunction]
    },
    {
        path: "create-user",
        component: CreateUserComponent,
        canActivate: [noAuthGuardFunction]
    },
    {
        path: "dashboard",
        component: DashboardComponent,
        canActivate: [authGuardFunction]
    },
    {
        path: "algorithm/:id",
        component: ManageAlgorithmComponent,
        canActivate: [authGuardFunction]
    },
    {
        path: "algorithm/new",
        component: ManageAlgorithmComponent,
        canActivate: [authGuardFunction]
    }
];

@NgModule({
    imports: [RouterModule.forRoot(routes, {useHash: true})],
    exports: [RouterModule]
})
export class AppRoutingModule { }
