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
    contentId?: string;
    datasetUri?: string;
};

export default function CommentSection(props: Props) {
    const { t } = useTranslation();
    const { contentId, datasetUri } = props;
    const [currentContentId, setCurrentContentId] = useState<string | undefined>(contentId);
    const [showNewCommentForm, setShowNewCommentForm] = useState<boolean>(false);

    const [comments, loading, error, load] = useCmsComments(currentContentId);

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
                                contentId={currentContentId}
                                datasetUri={datasetUri}
                                refresh={(newContentId) => {
                                    if (newContentId) {
                                        setCurrentContentId(newContentId);
                                    }
                                    load(newContentId ?? currentContentId);
                                    setShowNewCommentForm(false);
                                }}
                            />
                        )}
                    </GridColumn>
                    <GridColumn widthUnits={1} totalUnits={1}>
                        <SimpleList loading={loading} error={error}>
                            {comments?.map((comment: ICommentSorted, i: number) => (
                                <CommentElement key={`root-${i}`} contentId={currentContentId} comment={comment} refresh={() => load(currentContentId)} />
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
