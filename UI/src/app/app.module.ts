import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { LoaderComponent } from './loader/loader.component';
import { Client } from 'generated';
import { HttpClientModule  } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LoginComponent } from './login/login.component';
import { CreateUserComponent } from './create-user/create-user.component';
import { NavbarComponent } from './navbar/navbar.component';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatInputModule } from '@angular/material/input';
import { ChannelSearchComponent } from './channel-search/channel-search.component';
import { PlaylistSearchComponent } from './playlist-search/playlist-search.component';


import { ManageInvitesComponent } from './manage-invites/manage-invites.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { ManageAlgorithmComponent } from './manage-algorithm/manage-algorithm.component';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatTreeModule } from '@angular/material/tree';
import { MatCheckboxModule } from '@angular/material/checkbox';

@NgModule({
  declarations: [
    AppComponent,
    LoginComponent,
    CreateUserComponent,
    NavbarComponent,
    DashboardComponent,
    ManageAlgorithmComponent,
    ChannelSearchComponent,
    LoaderComponent,
    PlaylistSearchComponent,
    ManageInvitesComponent
  ],
  imports: [
    BrowserModule,
    CommonModule,
    FormsModule,
    AppRoutingModule,
    HttpClientModule,

    MatButtonModule,
    MatMenuModule,
    MatIconModule,
    MatInputModule,
    MatSnackBarModule,
    MatTableModule,
    MatTooltipModule,
    MatTreeModule,
    MatCheckboxModule
  ],
  providers: [Client, provideAnimationsAsync()],
  bootstrap: [AppComponent]
})
export class AppModule { }
