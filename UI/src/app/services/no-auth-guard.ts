// Import necessary modules and services
import { Injectable, inject } from '@angular/core';
import { Router, ActivatedRouteSnapshot, RouterStateSnapshot, CanActivateFn } from '@angular/router';
import { AuthService } from './auth.service'; // Assuming you have an AuthService for authentication
import { map, take } from 'rxjs';

export const noAuthGuardFunction: CanActivateFn = (
    next: ActivatedRouteSnapshot,
    state: RouterStateSnapshot) => {
        const authService = inject(AuthService);
        const router = inject(Router);
        return authService.getUserInfoAsync().pipe(take(1), map(userInfo => {
            if (userInfo.username == null){
                return true;
            } 
            router.navigate(["/dashboard"]);
            return false;
        }))
  }