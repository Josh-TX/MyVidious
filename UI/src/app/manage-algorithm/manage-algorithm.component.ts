import { Component, ViewChild } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { Client, FoundAlgorithm, FoundChannel, FoundPlaylist, UpdateAlgorithmRequest, UserInfo } from "generated";
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subscription } from "rxjs";
import { AuthService } from "../services/auth.service";
import { MatTable } from "@angular/material/table";
import { LoaderService } from "../services/loader.service";
import { getDistinct, getSum } from "../services/helpers"
import { AlgorithmService } from "../services/algorithms.service";


type Folder = {
    name: string;
    isExpanded: boolean;
    selected: boolean | null;
}

export type AlgorithmItem = {
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
    canEdit: boolean | undefined;
    isListed: boolean = true;
    copiedItems: AlgorithmItem[] = [];
    existingAlgorithms: FoundAlgorithm[] = [];
    private originalStateJson: string = "";

    algorithmId: number | undefined;

    @ViewChild("table") table!: MatTable<any>;

    displayedColumns: string[] = ['folder', 'type', 'name', 'count', 'weightMultiplier', 'weight', 'percent', 'select'];
    //displayedColumns: string[] = ['name', 'other', 'count'];
    private routeSub!: Subscription;
    ngOnInit() {
        this.routeSub = this.route.params.subscribe(params => {
            this.loader.setIsLoading(false);
            this.allItems = [];
            this.folders = [];
            this.name = "";
            this.description = "";
            this.tableRows = [];
            this.maxItemWeight = 100;
            this.originalStateJson = "";
            this.canEdit = true;
            this.isListed = true;
            this.algorithmId = undefined;
            if (params["id"] && parseInt(params["id"])) {
                this.algorithmId = parseInt(params["id"]);
                this.loadAlgorithm(this.algorithmId);
            } else {
                this.originalStateJson = this.getStateJson();
                this.allItems.unshift(...this.copiedItems);
                this.copiedItems = [];
                this.updateTableRows();
            }
        });
    }

    ngOnDestroy() {
        this.routeSub.unsubscribe();
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

    copyItemsToAlgorithm(algorithm: FoundAlgorithm | null){
        if (this.unsavedChanges()){
            if (!confirm("you have unsaved changes... exit anyways?")){
                return;
            }
        }
        this.copiedItems = this.allItems.filter(z => z.selected);
        this.copiedItems.forEach(z => {
            z.selected = false;
        })
        var part1 = `copied ${this.copiedItems.length} item${this.copiedItems.length > 1 ? 's' : ''}`;
        this.loader.setIsLoading(true);
        setTimeout(() => {
            if (!algorithm || !algorithm.algorithmId){
                this.snackBar.open(`${part1} to new algorithm`, "", { duration: 3000 });
                this.router.navigate(["/algorithm/new"]);
            } else {
                this.snackBar.open(`${part1} to algorithm "${algorithm.username}/${algorithm.algorithmName}"`, "", { duration: 3000 });
                this.router.navigate(["/algorithm", algorithm.algorithmId]);
            }
        }, 100);
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
        this.client.getOwnAlgorithms().subscribe(z => {
            this.existingAlgorithms = z.filter(z => z.algorithmId != algorithmId);
        });
        this.client.getAlgorithm(algorithmId).subscribe(result => {
            this.loader.setIsLoading(false);
            this.name = result.algorithmName!;
            this.canEdit = result.username == this.authService.getUserInfo()?.username;
            this.originalName = this.name;
            this.description = result.description!;
            this.maxItemWeight = result.maxItemWeight!;
            this.isListed = result.isListed!;
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
            this.originalStateJson = this.getStateJson();
            let badCopiedItems = this.copiedItems.filter(z => this.allItems.some(zz => zz.channelId == z.channelId || zz.playlistId == z.playlistId));
            if (badCopiedItems.length){
                //material doesn't support multiple snackbars, so delay 1.5 seconds so they have time to see the first snackbar
                setTimeout(() => {
                    this.snackBar.open(`${badCopiedItems.length} copied items are already on algorithm`, "", { panelClass: "snackbar-error", duration: 3000 });
                }, 1500)
            }
            this.allItems.unshift(...this.copiedItems.filter(z => !badCopiedItems.includes(z)));
            this.copiedItems = [];
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
        if (this.table){
            this.table.renderRows();
        }
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
        this.allItems.unshift({
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
        this.allItems.unshift({
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

    copyPath() {
        navigator.clipboard.writeText(this.getPath())
            .then(() => {
                this.snackBar.open("copied to clipboard", "", { duration: 3000 });
            })
            .catch(err => {
                this.snackBar.open("unable to copy to clipboard", "", { panelClass: "snackbar-error", duration: 3000 });
            });
    }

    private unsavedChanges(): boolean {
        return this.originalStateJson != this.getStateJson();
    }

    private getStateJson(): string {
        var itemsCopy: AlgorithmItem[] = JSON.parse(JSON.stringify(this.allItems));
        itemsCopy.forEach(z => z.selected = false);
        var state = {
            items: itemsCopy,
            list: this.isListed,
            name: this.name,
            desc: this.description,
            max: this.maxItemWeight
        };
        return JSON.stringify(state);
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
        if (!this.allItems.length) {
            this.snackBar.open("algorithm is empty", "", { panelClass: "snackbar-error", duration: 3000 });
            return;
        }
        var request: UpdateAlgorithmRequest = {
            algorithmId: this.algorithmId,
            name: this.name,
            description: this.description,
            maxItemWeight: this.maxItemWeight,
            isListed: this.isListed,
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
