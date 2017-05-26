﻿import { Component, EventEmitter, Input, Output, OnInit, OnDestroy } from '@angular/core';
import { Observable } from "rxjs/Rx";
import { TradeBotModel, TradeBotStates, TradeBotStateModel, ResponseStatus } from '../../models/index';
import { ApiService, ToastrService, FeedService } from '../../services/index';
import { Router } from '@angular/router';

@Component({
    selector: 'bot-card-cmp',
    template: require('./bot-card.component.html'),
    styles: [require('../../app.component.css')],
})

export class BotCardComponent implements OnInit {
    public TradeBotState = TradeBotStates;

    @Input() TradeBot: TradeBotModel;
    @Output() OnDeleted = new EventEmitter<TradeBotModel>();

    constructor(private _api: ApiService, private _toastr: ToastrService, private _router: Router) { }

    ngOnInit() {
        this._api.Feed.ChangeBotState
            .filter(botState => this.TradeBot && this.TradeBot.Id == botState.Id)
            .subscribe(botState => this.updateBotState(botState));
    }

    public get IsOnline(): boolean {
        return this.TradeBot.State === TradeBotStates.Online;
    }

    public get IsProcessing(): boolean {
        return this.TradeBot.State === TradeBotStates.Starting
            || this.TradeBot.State === TradeBotStates.Reconnecting
            || this.TradeBot.State === TradeBotStates.Stopping;
    }

    public get IsOffline(): boolean {
        return this.TradeBot.State === TradeBotStates.Offline;
    }

    public get Faulted(): boolean {
        return this.TradeBot.State === TradeBotStates.Faulted;
    }

    public get Broken(): boolean {
        return this.TradeBot.State === TradeBotStates.Broken;
    }

    public get CanStop(): boolean {
        return (this.TradeBot.State === TradeBotStates.Online
            || this.TradeBot.State === TradeBotStates.Starting
            || this.TradeBot.State === TradeBotStates.Reconnecting) && !this.Broken;
    }

    public get CanStart(): boolean {
        return (this.TradeBot.State === TradeBotStates.Offline
            || this.TradeBot.State === TradeBotStates.Faulted) && !this.Broken;
    }

    public get CanDelete(): boolean {
        return this.TradeBot.State === TradeBotStates.Offline
            || this.TradeBot.State === TradeBotStates.Faulted
            || this.Broken;
    }

    public get CanConfigurate(): boolean {
        return (this.TradeBot.State === TradeBotStates.Offline
            || this.TradeBot.State === TradeBotStates.Faulted) && !this.Broken;
    }

    public Start() {
        this.TradeBot = <TradeBotModel>{ ...this.TradeBot, 'State': TradeBotStates.Starting }

        this._api.StartBot(this.TradeBot.Id).subscribe(
            ok => { },
            err => this.notifyAboutError(err)
        );
    }

    public Stop() {
        this._api.StopBot(this.TradeBot.Id).subscribe(
            ok => { },
            err => this.notifyAboutError(err)
        );
    }

    public Delete(id: string, cleanLog: boolean, cleanAlgoData: boolean) {
        this._api.DeleteBot(id, cleanLog, cleanAlgoData).subscribe(
            ok => this.OnDeleted.emit(this.TradeBot),
            err => this.notifyAboutError(err)
        );
    }

    public GoToDetails(instanceId: string) {
        if (instanceId)
            this._router.navigate(['/bot', instanceId]);
    }

    public Configurate(instanceId: string) {
        if (instanceId)
            this._router.navigate(['/configurate', instanceId]);
    }

    private updateBotState(botState: TradeBotStateModel) {
        this.TradeBot = <TradeBotModel>{ ...this.TradeBot, 'State': botState.State, 'FaultMessage': botState.FaultMessage }
    }

    private notifyAboutError(response: ResponseStatus) {
        this._toastr.error(response.Message);
    }
}
