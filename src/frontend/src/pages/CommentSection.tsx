import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ICommentSorted, useCmsComments } from '../cms';
import Button from '../components/Button';
import GridColumn from '../components/GridColumn';
import GridRow from '../components/GridRow';
import SimpleList from '../components/SimpleList';
import { QueryGuard } from '../helpers/helpers';
import CommentElement from './CommentElement';
import CommentForm from './CommentForm';

type Props = {
    contentId: string;
};

export default function CommentSection(props: Props) {
    const { t } = useTranslation();
    const { contentId } = props;
    const [showNewCommentForm, setShowNewCommentForm] = useState<boolean>(false);

    const [comments, loading, error, load] = useCmsComments(contentId);

    return (
        <>
            <QueryGuard loading error data={comments}>
                <GridRow className="govuk-!-margin-top-8">
                    <GridColumn widthUnits={4} totalUnits={4}>
                        <GridRow>
                            <GridColumn widthUnits={1} totalUnits={2}>
                                <h2 className="govuk-heading-m govuk-!-margin-bottom-6 suggestion-subtitle">{t('comment.title')}</h2>
                            </GridColumn>
                            <GridColumn widthUnits={1} totalUnits={2} flexEnd>
                                {!showNewCommentForm && (
                                    <Button buttonType="primary" title={t('comment.new')} onClick={() => setShowNewCommentForm(true)}>
                                        {t('comment.new')}
                                    </Button>
                                )}
                            </GridColumn>
                        </GridRow>
                        {showNewCommentForm && (
                            <CommentForm
                                contentId={contentId}
                                refresh={() => {
                                    load(contentId);
                                    setShowNewCommentForm(false);
                                }}
                            />
                        )}
                    </GridColumn>
                    <GridColumn widthUnits={1} totalUnits={1}>
                        <SimpleList loading={loading} error={error} totalCount={comments?.length ?? 0}>
                            {comments?.map((comment: ICommentSorted, i: number) => (
                                <CommentElement key={`root-${i}`} contentId={contentId} comment={comment} refresh={() => load(contentId)} />
                            ))}
                        </SimpleList>
                    </GridColumn>
                </GridRow>
            </QueryGuard>
        </>
    );
}

CommentSection.defaultProps = {
    readonly: false
};
