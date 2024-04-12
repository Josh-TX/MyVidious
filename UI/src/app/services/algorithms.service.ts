import { Injectable } from '@angular/core';
import { Client, FoundAlgorithm, UserInfo } from 'generated';
import { BehaviorSubject, Observable, ReplaySubject } from 'rxjs';
import { AlgorithmItem } from '../manage-algorithm/manage-algorithm.component';

@Injectable({
    providedIn: 'root'
})
export class AlgorithmService {

    private _subject = new BehaviorSubject<boolean>(false)

    constructor(private client: Client) { 

    }

    private items: AlgorithmItem[] | undefined;
    private name: string | null = null;

    copyAlgorithmItems(algorithmName: string | null, items: AlgorithmItem[]){
        this.name = algorithmName;
        this.items = items;
        setTimeout(() => {
            this.items = undefined;
        }, 500);
    }

    getCopiedAlgorithmItems(algorithmName: string | null): AlgorithmItem[] {
        if (this.items != null && this.name == algorithmName){
            return this.items;
        }
        return [];
    }
}