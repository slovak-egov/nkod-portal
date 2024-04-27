import moment from 'moment';
import { Fragment, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ICommentSorted } from '../cms';
import Button from '../components/Button';
import GridColumn from '../components/GridColumn';
import GridRow from '../components/GridRow';
import { MAX_COOMENT_DEPTH_MARGIN_LEFT } from '../helpers/helpers';
import CommentForm from './CommentForm';

type Props = {
    contentId: string;
    comment: ICommentSorted;
    refresh: () => void;
};

export default function CommentElement(props: Props) {
    const { t } = useTranslation();
    const { comment, refresh, contentId } = props;
    const [showReplyForm, setShowReplyForm] = useState<boolean>(false);
    const marginLeft = `${(comment.depth < MAX_COOMENT_DEPTH_MARGIN_LEFT ? comment.depth : MAX_COOMENT_DEPTH_MARGIN_LEFT) * 45}px`;

    return (
        <>
            <Fragment key={comment.id}>
                <GridRow data-testid="sr-result" className="govuk-!-margin-bottom-6" style={{ marginLeft, borderLeft: '1px solid black' }}>
                    <GridColumn widthUnits={1} totalUnits={1}>
                        <GridRow>
                            <GridColumn widthUnits={3} totalUnits={4}>
                                <p className="govuk-body-m">
                                    <u>
                                        ({comment.email}) {comment.body}
                                    </u>
                                </p>
                            </GridColumn>
                            <GridColumn widthUnits={1} totalUnits={4} flexEnd>
                                <GridRow>
                                    <GridColumn widthUnits={1} totalUnits={1} flexEnd>
                                        {comment.created && <p className="govuk-body-s">{moment(comment.created).format('DD.MM.YYYY HH:mm:ss')}</p>}
                                    </GridColumn>
                                    <GridColumn widthUnits={1} totalUnits={1} flexEnd>
                                        <Button
                                            className="govuk-!-margin-bottom-#"
                                            buttonType="secondary"
                                            title={t('comment.reply')}
                                            onClick={() => setShowReplyForm(!showReplyForm)}
                                        >
                                            {t('comment.reply')}
                                        </Button>
                                    </GridColumn>
                                </GridRow>
                            </GridColumn>
                        </GridRow>
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
