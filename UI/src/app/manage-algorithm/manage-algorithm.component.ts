import { CommonModule } from "@angular/common";
import { Component, Input, ViewChild } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { Client, FoundChannel, UpdateAlgorithmRequest, UserInfo } from "generated";
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subscription } from "rxjs";
import { AuthService } from "../services/auth.service";
import { MatTable } from "@angular/material/table";



type AlgorithmItem = {
    channelGroupId?: number | undefined;
    channelId?: number | undefined;
    newChannel?: FoundChannel | undefined;
    weightMultiplier?: number;
    maxChannelWeight?: number;
    name?: string | undefined;
    status?: string | undefined;
}

@Component({
    selector: "manage-algorithm",
    templateUrl: './manage-algorithm.component.html'
})
export class ManageAlgorithmComponent {
    constructor(
        private client: Client, 
        private snackBar: MatSnackBar,
        private router: Router,
        private route: ActivatedRoute,
        private authService: AuthService
        ){
    }
    originalName: string = "";
    name: string = "";
    description: string = "";
    items: AlgorithmItem[] = [];
    algorithmId: number | undefined;

    @ViewChild("table") table!: MatTable<any>;

    displayedColumns: string[] = ['type', 'name', 'maxChannelWeight', 'weightMultiplier', 'status',  'actions'];
    private routeSub!: Subscription;
    ngOnInit(){
        this.routeSub = this.route.params.subscribe(params => {
            if (params["id"] && parseInt(params["id"])){
                this.algorithmId = parseInt(params["id"]);
                this.client.getAlgorithm(params["id"]).subscribe(result => {
                    this.name = result.algorithmName!;
                    this.originalName = this.name;
                    this.description = result.description!;
                    this.items = result.algorithmItems!.map(z => ({
                        name: z.name,
                        channelGroupId: z.channelGroupId,
                        channelId: z.channelId,
                        maxChannelWeight: z.maxChannelWeight,
                        weightMultiplier: z.weightMultiplier,
                    }));
                });
                if (this.table){
                    this.table.renderRows();
                }
            } else {
                this.algorithmId = undefined;
            }
        })
    }

    ngOnDestroy(){
        this.routeSub.unsubscribe();
    }

    getPath(): string{
        if (this.name){
            return document.location.origin + "/" + this.authService.getUserInfo()!.username + "/" + this.name;
        }
        return "";
    }

    addChannel(channel: FoundChannel){
        this.items.push({
            channelId: channel.channelId,
            newChannel: channel.channelId ? undefined : channel,
            weightMultiplier: 1,
            maxChannelWeight: 100,
            name: channel.name,
            status: "added"
        })
        this.table.renderRows();
    }
    remove(item: AlgorithmItem){
        this.items = this.items.filter(z => z != item);
        this.table.renderRows();
    }
    save(){
        if (!this.name){
            this.snackBar.open("algorithm name required", "", { panelClass: "snackbar-error", duration: 3000 });
            return;
        }
        if (!this.description){
            this.snackBar.open("algorithm description required", "", { panelClass: "snackbar-error", duration: 3000 });
            return;
        }
        if (!this.items.length){
            this.snackBar.open("algorithm is empty", "", { panelClass: "snackbar-error", duration: 3000 });
            return;
        }
        var request: UpdateAlgorithmRequest = {
            algorithmId: this.algorithmId,
            name: this.name,
            description: this.description,
            algorithmItems: this.items.map(z => ({
                channelGroupId: z.channelGroupId,
                channelId: z.channelId,
                newChannel: z.newChannel,
                maxChannelWeight: z.maxChannelWeight,
                weightMultiplier: z.weightMultiplier
            }))
        }
        this.client.updateAlgorithm(request).subscribe({
            next: id => {
                this.snackBar.open("Algorithm Saved");
                this.router.navigate(["/algorithm", id])
            },
            error: err => {
                this.snackBar.open(err, "", { panelClass: "snackbar-error", duration: 3000 });
            }
        })
    }
}

export type Item = {

}