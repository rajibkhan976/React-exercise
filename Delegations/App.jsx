import React from "react";
import { render } from "react-dom";
import { Provider } from "react-redux";
import { reduxStore as store } from "../../../index";
import * as delegationsActions from "../../../Actions/MyProfile/delegationsActions";
import * as translationService from "../../../Services/Translation/translationService";
import DelegationsList from './DelegationsList';

// Until we implement React router we will have to check if element exist.
let element = document.getElementById("myprofile-delegations");
if (element) {
    document.body.className += " force-nc-modal";
    // Early loading
    store.dispatch(translationService.loadTranslations("delegations.list")).then(() => {
        store.dispatch(delegationsActions.loadDelegationDetails());
        store.dispatch(delegationsActions.loadDelegationList("Id", "ASC"));
        
        render(
            <Provider store={store}>
                <DelegationsList />
            </Provider>,
            element
        );
    });
}
