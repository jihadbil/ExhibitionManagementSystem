export const normalizeAuditAction = (action: string): string => {
  switch (action.trim().toLowerCase()) {
    case 'added': case 'insert': case 'inserted': case 'create': case 'created': return 'Insert';
    case 'modified': case 'modify': case 'update': case 'updated': return 'Update';
    case 'deleted': case 'delete': return 'Delete';
    case 'approve': case 'approved': return 'Approve';
    case 'cancel': case 'cancelled': case 'canceled': return 'Cancel';
    default: return action;
  }
};
