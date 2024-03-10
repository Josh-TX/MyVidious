import { Injectable } from '@angular/core';
import { Client, UserInfo } from 'generated';
import { Observable, ReplaySubject } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class AuthService {

    private userInfoSubject = new ReplaySubject<UserInfo>(1);

    constructor(private client: Client) { 
        this.client.getUserInfo().subscribe(z => {
            this.userInfoSubject.next(z);
        });
    }

    getUserInfo(): Observable<UserInfo>{
        return this.userInfoSubject.asObservable();
    }

    setUserInfo(userInfo: UserInfo) {
        this.userInfoSubject.next(userInfo);
    }
}