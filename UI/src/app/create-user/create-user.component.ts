import { CommonModule } from "@angular/common";
import { Component } from "@angular/core";
import { Router } from "@angular/router";
import { Client } from "generated";
import { AuthService } from "../services/auth.service";
import { LoaderService } from "../services/loader.service";


@Component({
    templateUrl: './create-user.component.html'
})
export class CreateUserComponent {
    constructor(
        private client: Client, 
        private authService: AuthService, 
        private router: Router,
        private loader: LoaderService){
        this.authService.getUserInfoAsync().subscribe(z => this.isFirstUser = !z.anyUsers)
    }
    isFirstUser: boolean = false;
    username: string  = "";
    password: string = "";
    password2: string = "";
    error: string = "";
    create(){
        this.error = "";
        if (this.password != this.password2){
            this.error = "passwords don't match";
            return;
        }
        this.loader.setIsLoading(true);
        this.client.createUser({ username: this.username, password: this.password}).subscribe({
            next: userInfo => {
                this.authService.setUserInfo(userInfo);
                this.router.navigate(["/dashboard"], {replaceUrl: true});
            },
            error: error => {
                this.error = error;
            },
            complete: () => {
                this.loader.setIsLoading(false);
            }
        });
    }
}
