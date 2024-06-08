import moment from 'moment';
import { Fragment, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useDefaultHeaders, useUserPermissions } from '../client';
import { sendCmsDelete } from '../cms';
import Button from '../components/Button';
import GridColumn from '../components/GridColumn';
import GridRow from '../components/GridRow';
import { DATE_FORMAT, MAX_COOMENT_DEPTH_MARGIN_LEFT } from '../helpers/helpers';
import { ICommentSorted } from '../interface/cms.interface';
import CommentForm from './CommentForm';

type Props = {
    contentId?: string;
    comment: ICommentSorted;
    refresh: () => void;
};

export default function CommentElement(props: Props) {
    const { t } = useTranslation();
    const { isLogged, isSuperAdmin, isMine } = useUserPermissions();
    const headers = useDefaultHeaders();
    const { comment, refresh, contentId } = props;
    const [showReplyForm, setShowReplyForm] = useState<boolean>(false);
    const [showEditForm, setShowEditForm] = useState<boolean>(false);
    const marginLeft = `${(comment.depth < MAX_COOMENT_DEPTH_MARGIN_LEFT ? comment.depth : MAX_COOMENT_DEPTH_MARGIN_LEFT) * 45}px`;
    const canDelete = isSuperAdmin || (isLogged && isMine(comment.userId));
    const canEdit = isLogged && isMine(comment.userId);

    return (
        <>
            <Fragment key={comment.id}>
                <GridRow data-testid="sr-result" className="govuk-!-margin-bottom-6" style={{ marginLeft, borderLeft: '1px solid black' }}>
                    <GridColumn widthUnits={1} totalUnits={1}>
                        {showEditForm ? (
                            <GridRow>
                                <GridColumn widthUnits={4} totalUnits={4}>
                                    <CommentForm
                                        commentId={comment.id}
                                        refresh={() => {
                                            refresh();
                                            setShowEditForm(false);
                                        }}
                                        cancel={() => setShowEditForm(false)}
                                    />
                                </GridColumn>
                            </GridRow>
                        ) : (
                            <GridRow>
                                <GridColumn widthUnits={3} totalUnits={4}>
                                    <p className="govuk-body-m">
                                        (<b>{comment.email}</b>) {comment.body}
                                    </p>
                                </GridColumn>
                                <GridColumn widthUnits={1} totalUnits={4} flexEnd>
                                    <GridRow>
                                        <GridColumn widthUnits={1} totalUnits={1} flexEnd>
                                            {comment.created && <p className="govuk-body-s">{moment.utc(comment.created).local().format(DATE_FORMAT)}</p>}
                                        </GridColumn>
                                        <GridColumn widthUnits={1} totalUnits={1} flexEnd>
                                            {isLogged && (
                                                <Button
                                                    className="govuk-!-margin-bottom-#"
                                                    buttonType="secondary"
                                                    title={t('comment.reply')}
                                                    onClick={() => setShowReplyForm(!showReplyForm)}
                                                >
                                                    {t('comment.reply')}
                                                </Button>
                                            )}
                                            {canEdit && (
                                                <Button
                                                    className="govuk-!-margin-bottom-# govuk-!-margin-left-4"
                                                    buttonType="secondary"
                                                    title={t('comment.edit')}
                                                    onClick={() => setShowEditForm(true)}
                                                >
                                                    {t('comment.edit')}
                                                </Button>
                                            )}
                                            {canDelete && comment.children?.length === 0 && (
                                                <Button
                                                    className="govuk-!-margin-bottom-# govuk-!-margin-left-4"
                                                    buttonType="warning"
                                                    title={t('common.delete')}
                                                    onClick={async () => {
                                                        const result = await sendCmsDelete(`comments/${comment.id}`, headers);
                                                        if (result?.status === 200) {
                                                            refresh();
                                                        }
                                                    }}
                                                >
                                                    {t('common.delete')}
                                                </Button>
                                            )}
                                        </GridColumn>
                                    </GridRow>
                                </GridColumn>
                            </GridRow>
                        )}
                        <GridRow>
                            <GridColumn widthUnits={1} totalUnits={1}>
                                {showReplyForm && (
                                    <CommentForm
                                        contentId={contentId}
                                        parentId={comment.id}
                                        refresh={() => {
                                            refresh();
                                            setShowReplyForm(false);
                                        }}
                                    />
                                )}
                            </GridColumn>
                        </GridRow>
                    </GridColumn>
                </GridRow>
            </Fragment>
            {comment.children?.map((child: ICommentSorted, idx: number) => (
                <CommentElement contentId={contentId} comment={child} key={`ch-${idx}`} refresh={refresh} />
            ))}
        </>
    );
}

CommentElement.defaultProps = {
    comment: null
};
