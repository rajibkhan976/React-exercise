import React, { useEffect, useState } from "react";
import translate from "redux-polyglot/translate";
import { Modal } from "react-bootstrap";
import DelegationsForm from "./DelegationsForm";
import DelegationsReportingPlace from "./DelegationsReportingPlace";

const DelegationsModal = ({
    p,
    isLoading,
    isLoadingGroupTree,
    toggleModal,
    rowIndex,
    editIndex,
    deleteIndex,
    delegateRole,
    nodes,
    handleChangeReportingPlace,
    userName,
    startingDate,
    endingDate,
    validationError,
    handleChangeDelegateRole,
    handleChangeUserName,
    handleChangeStartingDate,
    handleChangeEndingDate,
    assignDelegation,
    editDelegation,
    updateDelegation,
    deleteDelegation,
    removeDelegation,
    handleModalClose,
    gridRef,
    registerChild
}) => {

    const saveDelegation = () => {
        if (editIndex !== null) {
            updateDelegation(editIndex, gridRef);
        } else {
            assignDelegation(registerChild);
        }
    }

    const ModalFormButtonGroup = ({ handleModalClose, saveDelegation }) => {
        return (
            <div className="col-xs-12">
                    <button type="button" className="btn btn-primary" onClick={() => saveDelegation()}>
                        {p.t("delegations_list_save")}
                    </button>
                    <button type="button" className="btn btn-default cancel-save-delegation-button" onClick={() => handleModalClose()}>
                        {p.t("delegations_list_cancel")}
                    </button>
            </div>
            );
    }

    return (
        <Modal
            show={toggleModal}
            onHide={handleModalClose}
            container={this}
            aria-labelledby="contained-modal-title"
            bsSize="lg"
            dialogClassName={
                (deleteIndex !== null) ?
                    "delegations-confirmation-modal modal-warning force-nc-modal"
                    :
                    "delegations-data-modal force-nc-modal"
                }
        >
            <Modal.Header closeButton>
                <Modal.Title id="contained-modal-title">{(deleteIndex !== null) ? p.t("delegations_list_confirmation_modal_header") : p.t("delegations_list_data_modal_header")}</Modal.Title>
            </Modal.Header>
            <Modal.Body className={(deleteIndex !== null) ? "delegations-confirmation-modal-body" : "delegations-data-modal-body"}>
                {(deleteIndex !== null) ?
                    <div>
                        <div className="delegations-confirmation-message">
                            {p.t("delegations_list_confirmation_message")}
                        </div>
                    </div>
                    :
                    <div className="delegations-data-modal-container">
                        <div className="row delegations-data-modal-row">
                            <div className="col-md-6 col-sm-12 delegations-data-modal-form">
                                <DelegationsForm
                                    editIndex={editIndex}
                                    delegateRole={delegateRole}
                                    userName={userName}
                                    startingDate={startingDate}
                                    endingDate={endingDate}
                                    handleChangeDelegateRole={handleChangeDelegateRole}
                                    handleChangeUserName={handleChangeUserName}
                                    handleChangeStartingDate={handleChangeStartingDate}
                                    handleChangeEndingDate={handleChangeEndingDate}
                                    assignDelegation={assignDelegation}
                                    updateDelegation={updateDelegation}
                                    handleModalClose={handleModalClose}
                                />
                            </div>
                            <div className="col-md-6 col-sm-12 delegations-data-modal-tree">
                                <DelegationsReportingPlace
                                    isLoading={isLoading}
                                    isLoadingGroupTree={isLoadingGroupTree}
                                    nodes={nodes}
                                    handleChangeReportingPlace={handleChangeReportingPlace}
                                />
                            </div>
                        </div>
                        <div className="delegations-form-validation-error">
                            {validationError ? 
                                <div>
                                    <span className="vismaicon vismaicon vismaicon-filled vismaicon-warning delegations-form-validation-icon" aria-hidden="true"></span>
                                    <div className="delegations-form-error-message">
                                        {validationError}
                                    </div>
                                </div>
                                :
                                null
                            }
                        </div>
                    </div>
                }
            </Modal.Body>
            <Modal.Footer>
                {(deleteIndex !== null) ?
                    <div className="delegations-confirmation-modal-buttons">
                        <button type="button" className="btn btn-danger" onClick={(event) => removeDelegation(deleteIndex, event)}>
                            {p.t("delegations_list_confirm_delete")}
                        </button>
                        <button type="button" className="btn btn-default cancel-remove-delegation-button" onClick={() => handleModalClose()}>
                            {p.t("delegations_list_cancel")}
                        </button>
                    </div>
                    :
                    <div className="row delegations-data-modal-buttons">
                        <ModalFormButtonGroup
                            handleModalClose={handleModalClose}
                            saveDelegation={saveDelegation}
                        />
                    </div>
                }
            </Modal.Footer>
        </Modal>
    );
};

export default translate(DelegationsModal);
