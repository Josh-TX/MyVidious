import { CommonModule } from "@angular/common";
import { Component } from "@angular/core";
import { Client } from "generated";
import { AuthService } from "../services/auth.service";
import { Router } from "@angular/router";


@Component({
    templateUrl: './login.component.html'
})
export class LoginComponent {
    constructor(private client: Client, private authService: AuthService, private router: Router){

    }

    username: string  = "";
    password: string = "";
    error: string = "";
    login(){
        this.error = "";
        this.client.login({ username: this.username, password: this.password}).subscribe({
            next: userInfo => {
                this.authService.setUserInfo(userInfo);
                this.router.navigate(["/dashboard"]);
            },
            error: error => {
                this.error = error;
            }
        });
    }
}
