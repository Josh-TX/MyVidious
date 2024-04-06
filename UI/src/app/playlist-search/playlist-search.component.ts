import { CommonModule } from "@angular/common";
import { Component, EventEmitter, Input, Output } from "@angular/core";
import { Router } from "@angular/router";
import { Client, FoundPlaylist } from "generated";
import { AuthService } from "../services/auth.service";
import { Subscription } from "rxjs";


@Component({
    selector: "playlist-search",
    templateUrl: './playlist-search.component.html'
})
export class PlaylistSearchComponent {
    @Input() excludeIds: number[] = []
    @Output("itemSelect") selectEmitter = new EventEmitter<FoundPlaylist>();

    constructor(private client: Client, private authService: AuthService, private router: Router){
        setInterval(() => {
            if (this.thumbCount < this.results.length - 1){
                this.thumbCount++;
            }
        }, 500)
    }
    text: string  = "";
    private timeoutId: any;
    private intervalId: any;
    showDropdown: boolean = false;
    isLoading: boolean = false;
    results: FoundPlaylist[] = [];
    thumbCount: number = 0;
    private subscription: Subscription | undefined
    private isFocused: boolean = false;
    private isMousedown: boolean = false;

    textChanged(){
        clearTimeout(this.timeoutId);
        clearInterval(this.intervalId);
        if (this.subscription){
            this.subscription.unsubscribe();
        }
        this.results = [];
        this.isLoading = false
        if (this.text.length > 0){
            this.showDropdown = true;
        }
        if (this.text.length > 1){
            this.isLoading = true; //yeah, I'm a liar... this is just a 1 second debounce time to lighten server load
            this.timeoutId = setTimeout(() => {
                this.subscription = this.client.searchPlaylists(this.text).subscribe({
                    next: results => {
                        this.results = results.filter(z => !this.excludeIds.includes(<any>z.playlistId));
                        this.isLoading = false;
                        this.showDropdown = true;
                        this.thumbCount = 0;
                        this.intervalId = setInterval(() => {
                            if (this.thumbCount < this.results.length - 1){
                                this.thumbCount++;
                            } else {
                                clearInterval(this.intervalId);
                            }
                        }, 25);
                    }
                })
            }, 500)
        }
    }  


    blured(){
        this.isFocused = false;
        setTimeout(() => {
            if (!this.isMousedown){
                this.showDropdown = false;
            }
        }, 10);
    }

    focused(){
        this.isFocused = true;
        this.isMousedown = false;
        if (this.text.length > 0){
            this.showDropdown = true;
        }
    }

    mousedown(){
        this.isMousedown = true;
    }

    mouseup(channel: FoundPlaylist ){
        console.log(channel);
        this.selectEmitter.emit(channel);
        this.text = "";
        this.isMousedown = false;
        this.showDropdown = false;
    }
}
