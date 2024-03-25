import { CommonModule } from "@angular/common";
import { Component, Input, ViewChild } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { Client, FoundChannel, UpdateAlgorithmRequest, UserInfo } from "generated";
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subscription } from "rxjs";
import { AuthService } from "../services/auth.service";
import { MatTable } from "@angular/material/table";
import { LoaderService } from "../services/loader.service";



type AlgorithmItem = {
    channelGroupId?: number | undefined;
    channelId?: number | undefined;
    newChannel?: FoundChannel | undefined;
    weightMultiplier?: number;
    maxChannelWeight?: number;
    name?: string | undefined;
    status?: string | undefined;
    videoCount: number
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
        private authService: AuthService,
        private loader: LoaderService
        ){
    }
    originalName: string = "";
    name: string = "";
    description: string = "";
    items: AlgorithmItem[] = [];
    algorithmId: number | undefined;
    weightExplanation = "The algorithm works by weighing channels, not videos. The channel's weight is equal to the # of videos (unless capped with max channel weight) multiplied by the weight multiplier.";

    @ViewChild("table") table!: MatTable<any>;

    displayedColumns: string[] = ['type', 'name', 'count', 'maxChannelWeight', 'weightMultiplier', 'weight', 'percent', 'actions'];
    private routeSub!: Subscription;
    ngOnInit(){
        this.routeSub = this.route.params.subscribe(params => {
            if (params["id"] && parseInt(params["id"])){
                this.algorithmId = parseInt(params["id"]);
                this.loadAlgorithm(this.algorithmId);
                if (this.table){
                    this.table.renderRows();
                }
            } else {
                this.algorithmId = undefined;
            }
        })
    }

    getPercent(item: AlgorithmItem): string{
        var sumWeight = this.items.map(z => this.getWeight(z)).reduce((p, a) => p + a, 0);
        var percent =  this.getWeight(item) / sumWeight
        return (Math.round(percent * 1000) / 10) + " %";
    }

    getWeight(item: AlgorithmItem): number{
        var videoCount = item.videoCount > 0 || item.channelId != null ? item.videoCount : 100;
        return Math.min(videoCount, 100) * Math.max(0, item.weightMultiplier || 0);
    }

    isGuess(item: AlgorithmItem): boolean{
        return !(item.videoCount > 0 || item.channelId != null);
    }

    private loadAlgorithm(algorithmId: number){
        this.loader.setIsLoading(true);
        this.client.getAlgorithm(algorithmId).subscribe(result => {
            this.loader.setIsLoading(false);
            this.name = result.algorithmName!;
            this.originalName = this.name;
            this.description = result.description!;
            this.items = result.algorithmItems!.map(z => ({
                name: z.name,
                channelGroupId: z.channelGroupId,
                channelId: z.channelId,
                maxChannelWeight: z.maxChannelWeight,
                weightMultiplier: z.weightMultiplier,
                videoCount: z.videoCount || 0
            }));
        });
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
            name: channel.author,
            status: "added",
            videoCount: channel.videoCount!,
        })
        this.table.renderRows();
    }
    remove(item: AlgorithmItem){
        this.items = this.items.filter(z => z != item);
        this.table.renderRows();
    }

    copy(){
        navigator.clipboard.writeText(this.getPath())
            .then(() => {
                this.snackBar.open("copied to clipboard", "", { duration: 3000 });
            })
            .catch(err => {
                this.snackBar.open("unable to copy to clipboard", "", { panelClass: "snackbar-error", duration: 3000 });
            });
    }

    save(){
        if (!this.name){
            this.snackBar.open("algorithm name required", "", { panelClass: "snackbar-error", duration: 3000 });
            return;
        }
        if (!/^[a-zA-Z0-9]+$/.test(this.name)){
            this.snackBar.open("algorithm name must be alphanumeric", "", { panelClass: "snackbar-error", duration: 3000 });
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
        this.loader.setIsLoading(true);
        this.client.updateAlgorithm(request).subscribe({
            next: id => {
                this.snackBar.open("Algorithm Saved. Changes may take a few minutes to take effect on the API", "", { duration: 3000 });
                if (!this.algorithmId){
                    this.router.navigate(["/algorithm", id])
                } else {
                    this.loadAlgorithm(this.algorithmId);
                }
            },
            error: err => {
                this.snackBar.open(err, "", { panelClass: "snackbar-error", duration: 3000 });
                this.loader.setIsLoading(false);
            }
        })
    }

    delete(){
        this.loader.setIsLoading(true);
        if (confirm("Are you sure you want to delete this algorithm?") && this.algorithmId){
            this.client.deleteAlgorithm(this.algorithmId).subscribe(z => {
                this.snackBar.open("Algorithm Deleted", "", { duration: 3000 });
                this.router.navigate(["/"])
            })
        }
    }
}

export type Item = {

}