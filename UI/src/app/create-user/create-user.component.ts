import { CommonModule } from "@angular/common";
import { Component } from "@angular/core";
import { Router } from "@angular/router";
import { Client } from "generated";
import { AuthService } from "../services/auth.service";


@Component({
    templateUrl: './create-user.component.html'
})
export class CreateUserComponent {
    constructor(private client: Client, private authService: AuthService, private router: Router){
        this.authService.getUserInfo().subscribe(z => this.isFirstUser = !z.anyUsers)
    }
    isFirstUser: boolean = false;
    username: string  = "";
    password: string = "";
    error: string = "";
    create(){
        this.error = "";
        this.client.createUser({ username: this.username, password: this.password}).subscribe({
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
