import { CommonModule } from "@angular/common";
import { Component } from "@angular/core";
import { Router } from "@angular/router";
import { Client, UserInfo } from "generated";
import { AuthService } from "../services/auth.service";
import { Observable } from "rxjs";
import { LoaderService } from "../services/loader.service";


@Component({
    selector: "loader",
    templateUrl: './loader.component.html'
})
export class LoaderComponent {

    isLoading: Observable<boolean>

    constructor(private loaderService: LoaderService){
        this.isLoading = this.loaderService.getIsLoading();
    }
}
