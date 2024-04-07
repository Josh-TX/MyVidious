import { Component, ViewChild } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { Client, FoundChannel, FoundPlaylist, UpdateAlgorithmRequest, UserInfo } from "generated";
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subscription } from "rxjs";
import { AuthService } from "../services/auth.service";
import { MatTable } from "@angular/material/table";
import { LoaderService } from "../services/loader.service";
import { getDistinct, getSum } from "../services/helpers"


type Folder = {
    name: string;
    isExpanded: boolean;
    selected: boolean | null;
}

type AlgorithmItem = {
    channelId?: number | undefined;
    newChannel?: FoundChannel | undefined;
    playlistId?: number | undefined;
    newPlaylist?: FoundPlaylist | undefined;
    weightMultiplier?: number;
    name?: string | undefined;
    videoCount: number;
    selected: boolean;
    uniqueId: string;
    folderName?: string | undefined
}

type TableRow = Folder | AlgorithmItem

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
    ) {
    }
    originalName: string = "";
    name: string = "";
    description: string = "";
    allItems: AlgorithmItem[] = [];
    folders: Folder[] = [];
    tableRows: TableRow[] = [];
    maxItemWeight: number = 100;

    algorithmId: number | undefined;

    @ViewChild("table") table!: MatTable<any>;

    displayedColumns: string[] = ['folder', 'type', 'name', 'count', 'weightMultiplier', 'weight', 'percent', 'select'];
    //displayedColumns: string[] = ['name', 'other', 'count'];
    private routeSub!: Subscription;
    ngOnInit() {
        this.routeSub = this.route.params.subscribe(params => {
            if (params["id"] && parseInt(params["id"])) {
                this.algorithmId = parseInt(params["id"]);
                this.loadAlgorithm(this.algorithmId);
                if (this.table) {
                    this.table.renderRows();
                }
            } else {
                this.algorithmId = undefined;
            }
        });
        // setInterval(() => {
        //     console.log(this.allItems.map(z => z.name + " " + z.selected))
        // })
    }


    getPercent(item: AlgorithmItem): string {
        var sumWeight = this.allItems.map(z => this.getWeight(z)).reduce((p, a) => p + a, 0);
        var percent = this.getWeight(item) / sumWeight
        return (Math.round(percent * 1000) / 10) + " %";
    }

    getWeight(row: TableRow): number {
        if (this.isFolder(row)){
            var folderItems = this.allItems.filter(z => z.folderName == row.name);
            return getSum(folderItems.map(z => this.getItemWeight(z)));
        } else {
            return this.getItemWeight(row);
        }
    }

    private getItemWeight(item: AlgorithmItem){
        var videoCount = item.videoCount > 0 || item.channelId != null ? item.videoCount : 100;
        return Math.min(videoCount, this.maxItemWeight) * Math.max(0, item.weightMultiplier || 0);
    }

    isGuess(item: AlgorithmItem): boolean {
        return !(item.videoCount > 0 || item.channelId != null);
    }

    private loadAlgorithm(algorithmId: number) {
        this.loader.setIsLoading(true);
        this.client.getAlgorithm(algorithmId).subscribe(result => {
            this.loader.setIsLoading(false);
            this.name = result.algorithmName!;
            this.originalName = this.name;
            this.description = result.description!;
            this.maxItemWeight = result.maxItemWeight!;
            this.allItems = result.algorithmItems!.map(z => ({
                name: z.name,
                channelId: z.channelId,
                playlistId: z.playlistId,
                weightMultiplier: z.weightMultiplier,
                videoCount: z.videoCount || 0,
                uniqueId: z.uniqueId!,
                selected: false,
                folderName: z.folder
            }));
            this.updateTableRows();
        });
    }

    private updateTableRows() {
        this.tableRows = [];
        var oldFolders = this.folders;
        this.folders = getDistinct(this.allItems.map(z => z.folderName).filter(z => !!z)).map(folderName => {
            var foundFolder = oldFolders.find(z => z.name == folderName);
            if (foundFolder) {
                return foundFolder;
            } else {
                return {
                    name: folderName!,
                    isExpanded: false,
                    selected: this.isAllSelected()
                }
            }
        });
        for (var folder of this.folders) {
            this.tableRows.push(folder);
            if (folder.isExpanded) {
                this.tableRows.push(...this.allItems.filter(z => z.folderName == folder.name));
            }
        }
        this.tableRows.push(...this.allItems.filter(z => z.folderName == null));
        this.table.renderRows();
    }


    ngOnDestroy() {
        this.routeSub.unsubscribe();
    }

    getPath(): string {
        if (this.name) {
            return document.location.origin + "/" + this.authService.getUserInfo()!.username + "/" + this.name;
        }
        return "";
    }

    addChannel(channel: FoundChannel) {
        if (this.allItems.some(z => z.uniqueId == channel.authorId)){
            this.snackBar.open("channel already exists on algorithm", "", { panelClass: "snackbar-error", duration: 3000 });
            return;
        }
        this.allItems.push({
            channelId: channel.channelId,
            newChannel: channel.channelId ? undefined : channel,
            weightMultiplier: 1,
            name: channel.author,
            uniqueId: channel.authorId!,
            videoCount: channel.videoCount!,
            selected: this.isAllSelected()
        })
        this.updateTableRows();
    }
    addPlaylist(playlist: FoundPlaylist) {
        if (this.allItems.some(z => z.uniqueId == playlist.playlistId)){
            this.snackBar.open("playlist already exists on algorithm", "", { panelClass: "snackbar-error", duration: 3000 });
            return;
        }
        this.allItems.push({
            playlistId: playlist.myvidiousPlaylistId,
            newPlaylist: playlist.myvidiousPlaylistId ? undefined : playlist,
            weightMultiplier: 1,
            name: playlist.title,
            uniqueId: playlist.playlistId!,
            videoCount: playlist.videoCount!,
            selected: this.isAllSelected()
        })
        this.updateTableRows();
    }
    removeSelected() {
        this.allItems = this.allItems.filter(z => !z.selected);
        this.updateTableRows();
    }

    copy() {
        navigator.clipboard.writeText(this.getPath())
            .then(() => {
                this.snackBar.open("copied to clipboard", "", { duration: 3000 });
            })
            .catch(err => {
                this.snackBar.open("unable to copy to clipboard", "", { panelClass: "snackbar-error", duration: 3000 });
            });
    }

    save() {
        if (!this.name) {
            this.snackBar.open("algorithm name required", "", { panelClass: "snackbar-error", duration: 3000 });
            return;
        }
        if (!/^[a-zA-Z0-9]+$/.test(this.name)) {
            this.snackBar.open("algorithm name must be alphanumeric", "", { panelClass: "snackbar-error", duration: 3000 });
            return;
        }
        // if (!this.description) {
        //     this.snackBar.open("algorithm description required", "", { panelClass: "snackbar-error", duration: 3000 });
        //     return;
        // }
        if (!this.allItems.length) {
            this.snackBar.open("algorithm is empty", "", { panelClass: "snackbar-error", duration: 3000 });
            return;
        }
        var request: UpdateAlgorithmRequest = {
            algorithmId: this.algorithmId,
            name: this.name,
            description: this.description,
            maxItemWeight: this.maxItemWeight,
            algorithmItems: this.allItems.map(z => ({
                channelId: z.channelId,
                newChannel: z.newChannel,
                playlistId: z.playlistId,
                newPlaylist: z.newPlaylist,
                weightMultiplier: z.weightMultiplier,
                folder: z.folderName
            }))
        }
        this.loader.setIsLoading(true);
        this.client.updateAlgorithm(request).subscribe({
            next: id => {
                this.snackBar.open("Algorithm Saved. Changes may take a few minutes to take effect on the API", "", { duration: 3000 });
                if (!this.algorithmId) {
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

    delete() {
        if (confirm("Are you sure you want to delete this algorithm?") && this.algorithmId) {
            this.loader.setIsLoading(true);
            this.client.deleteAlgorithm(this.algorithmId).subscribe(z => {
                this.snackBar.open("Algorithm Deleted", "", { duration: 3000 });
                this.router.navigate(["/"])
            })
        }
    }

    addToFolder(existingFolder: Folder | undefined = undefined) {
        var name = existingFolder ? existingFolder.name : prompt("new folder name");
        if (!name) {
            return;
        }
        this.allItems.filter(z => z.selected).forEach(z => z.folderName = name!);
        this.updateTableRows();
        this.allItems.forEach(z => z.selected = false);
        this.folders.forEach(z => z.selected = false);
    }

    toggleSelected(tableRow: TableRow) {
        if (this.isFolder(tableRow)) {
            if (this.isFolderSelected(tableRow)) {
                tableRow.selected = false;
            } else {
                tableRow.selected = true;
            }
            this.allItems.filter(z => z.folderName == tableRow.name).forEach(z => z.selected = tableRow.selected!);
        } else {
            tableRow.selected = !tableRow.selected;
            var folder = this.folders.find(z => z.name == tableRow.folderName);
            if (folder) {
                var items = this.allItems.filter(z => z.folderName == folder!.name);
                var selected = items.filter(z => z.selected);
                console.log(folder, items, selected)
                if (selected.length && selected.length != items.length) {
                    folder.selected = null;
                } else {
                    folder.selected = !!selected.length;
                }
            }
        }
    }

    private isFolderSelected(folder: Folder): boolean {
        return this.allItems.filter(z => z.folderName == folder.name).every(z => z.selected);
    }

    toggleExpanded(item: TableRow) {
        var folder = item as Folder;
        folder.isExpanded = !folder.isExpanded;
        this.updateTableRows();
    }

    isFolder(row: TableRow): row is Folder {
        return "isExpanded" in row;
    }

    getTypeName(row: TableRow) {
        if (this.isFolder(row)) {
            return "folder";
        } else if (row.playlistId || row.newPlaylist) {
            return "playlist";
        } else {
            return "channel";
        }
    }

    isAllSelected(): boolean {
        return this.allItems.length > 0 && this.allItems.every(z => z.selected)
    }
    isIndeterminate(): boolean {
        return this.allItems.some(z => z.selected) && this.allItems.some(z => !z.selected)
    }

    getSelectedCount(): number {
        return this.allItems.filter(z => z.selected).length
    }


    toggleAllRows() {
        if (this.isAllSelected()) {
            this.allItems.forEach(z => z.selected = false);
            this.folders.forEach(z => z.selected = false);
        } else {
            this.allItems.forEach(z => z.selected = true);
            this.folders.forEach(z => z.selected = true);
        }
    }
}
