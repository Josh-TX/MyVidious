import { CommonModule } from "@angular/common";
import { Component } from "@angular/core";
import { Client, FoundAlgorithm } from "generated";
import { AuthService } from "../services/auth.service";


@Component({
    templateUrl: './dashboard.component.html'
})
export class DashboardComponent {
    constructor(private client: Client, private authService: AuthService){
        var username = this.authService.getUserInfo()!.username;
        this.client.searchAlgorithms(username).subscribe(results => this.algorithms = results)
    }
    algorithms: FoundAlgorithm[] = [];
}
