import { Injectable } from '@angular/core';
import { Client, UserInfo } from 'generated';
import { BehaviorSubject, Observable, ReplaySubject } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class LoaderService {

    private _subject = new BehaviorSubject<boolean>(false)

    constructor() { 

    }

    getIsLoading(){
        return this._subject.asObservable()
    }

    setIsLoading(isLoading: boolean){
        this._subject.next(isLoading);
    }
}