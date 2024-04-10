import { CommonModule } from "@angular/common";
import { Component, ViewChild } from "@angular/core";
import { Client, FoundAlgorithm, InviteCode } from "generated";
import { AuthService } from "../services/auth.service";
import { generateRandomCode } from "../services/helpers";
import { MatTable } from "@angular/material/table";
import { MatSnackBar } from "@angular/material/snack-bar";


@Component({
    templateUrl: './manage-invites.component.html'
})
export class ManageInvitesComponent {

    invites?: InviteCode[] | undefined;
    openInvite: boolean = true;
    displayedColumns: string[] = ['code', 'remaining', 'used', 'actions'];
    @ViewChild("table") table!: MatTable<any>;

    constructor(
        private client: Client,
        private snackBar: MatSnackBar
    ){
        this.client.getInviteCodes().subscribe({
            next: results => this.update(results),
            error: err => alert("invalid permissions for this page")
        });
    }
    private update(invites: InviteCode[]){
        if (invites.some(z => !z.code)){
            this.openInvite = true;
            this.invites = [];
        } else {
            this.openInvite = false;
            this.invites = invites.filter(z => z.remainingUses! > 0);
        }
    }

    remove(item: InviteCode){
        this.invites = this.invites!.filter(z => z != item);
        setTimeout(() => {
            this.table.renderRows();
        })
    }

    add(){
        this.invites = this.invites || [];
        this.invites.push({
            code: "inv-" + generateRandomCode(10),
            remainingUses: 1,
            usageCount: 0
        });
        setTimeout(() => {
            this.table.renderRows();
        })
    }

    save(){
        var inviteCodes = this.openInvite 
            ? [{code: "", remainingUses: 1000000, usageCount: 0}]
            : this.invites!.filter(z => z.remainingUses! > 0);
        this.client.updateInviteCodes(inviteCodes).subscribe(() => {
            this.snackBar.open("Changes Saved", "", { duration: 3000 });
        })
    }
}
