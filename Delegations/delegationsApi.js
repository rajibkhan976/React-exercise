import apiClient from "./../../Utils/apiClient";
const rootPrefix = "/api/v1/Delegations";

export function getDelegationList(sort, sortDir) {
    return apiClient.get(`${rootPrefix}/`, {
        params: {
            sort,
            sortDir
        }
    });
}

export function getDelegationDetails(id) {
    return apiClient.get(`${rootPrefix}/${id}`);
}

export function getJsonGroupTree(selectedUserRoleId, destinationUserRoleId) {
    return apiClient.get(`${rootPrefix}/group-tree`, {
        params: {
            selectedUserRoleId,
            destinationUserRoleId
        }
    });
}

export function getDependsOnSelectedRolePartial(selectedUserRoleId) {
    return apiClient.get(`${rootPrefix}/getDependsOnSelectedRolePartial`, {
        params: {
            selectedUserRoleId
        }
    });
}

export function getDelegationUsers(userRoleId, searchTerm) {
    return apiClient.get(`${rootPrefix}/users`, {
        params: {
            userRoleId,
            searchTerm
        }
    });
}

export function saveDelegation(
    userRoleId,
    delegationId,
    userId,
    startDate,
    endDate,
    groupCheckedIds
) {
    return apiClient.post(
        `${rootPrefix}/`,
        {
            delegationId,
            userId,
            startDate,
            endDate,
            groupCheckedIds
        },
        {
            params: {
                userRoleId
            }
        }
    );
}

export function deleteDelegation(id) {
    return apiClient.delete(`${rootPrefix}/${id}`);
}
