import { CommonModule } from "@angular/common";
import { Component } from "@angular/core";
import { Client } from "generated";
import { AuthService } from "../services/auth.service";
import { Router } from "@angular/router";
import { LoaderService } from "../services/loader.service";


@Component({
    templateUrl: './login.component.html'
})
export class LoginComponent {
    constructor(
        private client: Client, 
        private authService: AuthService, 
        private router: Router,
        private loader: LoaderService){

    }

    username: string  = "";
    password: string = "";
    error: string = "";
    login(){
        this.loader.setIsLoading(true);
        this.error = "";
        this.client.login({ username: this.username, password: this.password}).subscribe({
            next: userInfo => {
                this.authService.setUserInfo(userInfo);
                this.router.navigate(["/dashboard"], {replaceUrl: true});
            },
            error: error => {
                this.error = "Invalid Credentials";
            },
            complete: () => {
                this.loader.setIsLoading(false);
            }
        });
    }
}
