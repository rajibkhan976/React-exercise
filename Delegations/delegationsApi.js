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

export function getDelegableRoles(roleId) {
    return apiClient.get(`${rootPrefix}/findDelegableRoles`, { params: { roleId } });
}

export function getJsonGroupTree(selectedUserRoleId, destinationUserRoleId) {
    return apiClient.get(`${rootPrefix}/group-tree`, {
        params: {
            selectedUserRoleId,
            destinationUserRoleId
        }
    });
}

export function getAllowedGroupTree(selectedUserRoleId) {
    return apiClient.get(`${rootPrefix}/findAllowedGroupTree`, {
        params: {
            selectedUserRoleId
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

export function getDelegableUsers(roleId) {
    return apiClient.get(`${rootPrefix}/findDelegableUsers`, {
        params: {
            roleId
        }
    });
}

export function findDelegatedUser(id) {
    return apiClient.get(`${rootPrefix}/findDelegatedUser/${id}`);
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
