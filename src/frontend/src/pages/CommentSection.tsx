import { yupResolver } from '@hookform/resolvers/yup';
import moment from 'moment';
import { Fragment, useState } from 'react';
import { SubmitHandler, useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { buildYup } from 'schema-to-yup';
import { useUserInfo } from '../client';
import { Comment, CommentFormValues, sendPost, useCmsComments } from '../cms';
import Button from '../components/Button';
import FormElementGroup from '../components/FormElementGroup';
import GridColumn from '../components/GridColumn';
import GridRow from '../components/GridRow';
import SimpleList from '../components/SimpleList';
import TextArea from '../components/TextArea';
import { schema, schemaConfig } from './schemas/CommentSchema';

// TODO:
// Focus na komentáre po kliku zo zoznamu

type Props = {
    contentId: string;
};

export default function CommentSection(props: Props) {
    const { t } = useTranslation();
    const [userInfo] = useUserInfo();
    const { contentId } = props;
    const [showForm, setShowForm] = useState<boolean>(false);
    const yupSchema = buildYup(schema, schemaConfig);

    const form = useForm<CommentFormValues>({
        resolver: yupResolver(yupSchema),
        defaultValues: {
            body: ''
        }
    });

    const [comments, loading, error, load] = useCmsComments(contentId);

    const {
        register,
        handleSubmit,
        reset,
        formState: { errors }
    } = form;

    const onSubmit: SubmitHandler<CommentFormValues> = async (data) => {
        const request = {
            contentId,
            userId: userInfo?.id || '0b33ece7-bbff-4ae6-8355-206cb5b1aa87',
            email: userInfo?.email || 'test@email.sk',
            body: data.body
        };
        const result = await sendPost<any>(`cms/comments`, request);
        if (result.status === 200) {
            load(contentId);
            reset();
            setShowForm(false);
        }
    };

    const onErrors = () => {
        console.error(errors);
    };

    return (
        <>
            {/* <QueryGuard {...loadFormData}> */}
            {/* {watch() && JSON.stringify(form.getValues())} */}
            <GridRow className="govuk-!-margin-top-8">
                <GridColumn widthUnits={4} totalUnits={4}>
                    <GridRow>
                        <GridColumn widthUnits={1} totalUnits={2}>
                            <h2 className="govuk-heading-m govuk-!-margin-bottom-6 suggestion-subtitle">{t('comment.title')}</h2>
                        </GridColumn>
                        <GridColumn widthUnits={1} totalUnits={2} flexEnd>
                            {!showForm && (
                                <Button buttonType="primary" title={t('comment.new')} onClick={() => setShowForm(true)}>
                                    {t('comment.new')}
                                </Button>
                            )}
                        </GridColumn>
                    </GridRow>
                    {showForm && (
                        <form onSubmit={handleSubmit(onSubmit, onErrors)}>
                            <FormElementGroup label={t('comment.comment')} element={(id) => <TextArea id={id} rows={3} {...register('body')} />} />
                            <Button buttonType="primary" title={t('comment.add')}>
                                {t('comment.add')}
                            </Button>
                        </form>
                    )}
                </GridColumn>
                <GridColumn widthUnits={1} totalUnits={1}>
                    <SimpleList loading={loading} error={error} totalCount={comments?.length ?? 0}>
                        {comments?.map((comment: Comment, i: number) => (
                            <Fragment key={comment.id}>
                                <GridRow data-testid="sr-result">
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
                                                {comment.created && <p className="govuk-body-s">{moment(comment.created).format('DD.MM.YYYY HH:mm:ss')}</p>}
                                            </GridColumn>
                                        </GridRow>
                                    </GridColumn>
                                </GridRow>
                                {i < comments.length - 1 ? <hr className="idsk-search-results__card__separator" /> : null}
                            </Fragment>
                        ))}
                    </SimpleList>
                </GridColumn>
            </GridRow>
            {/* </QueryGuard> */}
        </>
    );
}

CommentSection.defaultProps = {
    readonly: false
};
