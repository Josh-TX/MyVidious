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
        this.authService.getUserInfoAsync().subscribe(z => {
            this.isFirstUser = !z.anyUsers;
            this.openInvite = z.openInvite;
        })
    }
    isFirstUser: boolean = false;
    openInvite: boolean | undefined;
    inviteCode: string = "";
    username: string  = "";
    password: string = "";
    password2: string = "";
    error: string = "";
    create(){
        this.error = "";
        if (this.openInvite == null && !this.inviteCode){
            this.error = "invite code required";
            return;
        }
        if (!this.username){
            this.error = "username required";
            return;
        }
        if (!this.password){
            this.error = "password required";
            return;
        }
        if (this.password != this.password2){
            this.error = "passwords don't match";
            return;
        }
        this.loader.setIsLoading(true);
        this.client.createUser({ username: this.username, password: this.password, inviteCode: this.inviteCode}).subscribe({
            next: userInfo => {
                this.authService.setUserInfo(userInfo);
                this.router.navigate(["/dashboard"], {replaceUrl: true});
            },
            error: error => {
                this.loader.setIsLoading(false);
                this.error = error;
            },
            complete: () => {
                this.loader.setIsLoading(false);
            }
        });
    }
}
