import { CommonModule } from "@angular/common";
import { Component } from "@angular/core";
import { Router } from "@angular/router";
import { Client, UserInfo } from "generated";
import { AuthService } from "../services/auth.service";


@Component({
    selector: "navbar",
    templateUrl: './navbar.component.html'
})
export class NavbarComponent {
    constructor(private authService: AuthService, private router: Router){
        this.authService.getUserInfo().subscribe(z => this.userInfo = z)
    }
    userInfo: UserInfo | undefined  
}
