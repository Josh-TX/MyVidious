import { Component } from "@angular/core";
import { Client, FoundAlgorithm } from "generated";


@Component({
    templateUrl: './dashboard.component.html'
})
export class DashboardComponent {
    constructor(private client: Client){
        this.client.getOwnAlgorithms().subscribe(results => this.ownAlgorithms = results)
        this.client.getOthersAlgorithms().subscribe(results => this.othersAlgorithms = results)
    }
    ownAlgorithms: FoundAlgorithm[] | undefined;
    othersAlgorithms: FoundAlgorithm[] = [];
}
