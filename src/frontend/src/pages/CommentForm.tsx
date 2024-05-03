import { yupResolver } from '@hookform/resolvers/yup';
import { SubmitHandler, useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { buildYup } from 'schema-to-yup';
import { useDefaultHeaders, useUserInfo } from '../client';
import { sendCmsPost, sendCmsPut } from '../cms';
import Button from '../components/Button';
import FormElementGroup from '../components/FormElementGroup';
import TextArea from '../components/TextArea';
import { QueryGuard, ROOT_ID, useLoadData, useSchemaConfig } from '../helpers/helpers';
import { CommentFormValues, IComment } from '../interface/cms.interface';
import { schema } from './schemas/CommentSchema';

type Props = {
    contentId?: string;
    parentId?: string;
    commentId?: string;
    datasetUri?: string;
    refresh: (newContentId?: string) => void;
    cancel?: () => void;
};

export default function CommentForm(props: Props) {
    const { t } = useTranslation();
    const [userInfo] = useUserInfo();
    const headers = useDefaultHeaders();
    const { contentId, refresh, parentId, datasetUri, cancel, commentId } = props;
    const yupSchema = buildYup(schema, useSchemaConfig(schema.required));

    const form = useForm<CommentFormValues>({
        resolver: yupResolver(yupSchema),
        defaultValues: {
            body: ''
        }
    });

    const loadFormData = useLoadData<any, IComment>({
        disabled: !commentId,
        form,
        url: `comments/${commentId}`
    });

    const {
        register,
        handleSubmit,
        reset,
        formState: { errors }
    } = form;

    const onSubmit: SubmitHandler<CommentFormValues> = async (data) => {
        let result;
        if (!contentId && datasetUri) {
            const request = {
                datasetUri,
                userId: userInfo?.id,
                email: userInfo?.email,
                body: data.body
            };
            result = await sendCmsPost<any>(`datasets/comments`, request, headers);
            if (result?.status === 200) {
                // post datasets/comments returns new CmsDataset, which will be used for
                refresh(result.data);
                reset();
            }
        } else {
            if (!commentId) {
                const request = {
                    contentId,
                    parentId: parentId ?? ROOT_ID,
                    userId: userInfo?.id,
                    email: userInfo?.email,
                    body: data.body
                };
                result = await sendCmsPost<any>(`comments`, request, headers);
            } else {
                result = await sendCmsPut<any>(`comments/${commentId}`, data, headers);
            }
            if (result?.status === 200) {
                refresh();
                reset();
            }
        }
    };

    const onErrors = () => {
        console.error(errors);
    };

    return (
        <>
            <QueryGuard {...loadFormData} isNew={!commentId}>
                <form onSubmit={handleSubmit(onSubmit, onErrors)}>
                    <FormElementGroup label={t('comment.comment')} element={(id) => <TextArea id={id} rows={3} {...register('body')} />} />
                    <Button type={'submit'} buttonType="primary" title={t(commentId ? 'comment.edit' : 'comment.add')}>
                        {t(commentId ? 'comment.edit' : 'comment.add')}
                    </Button>
                    {cancel && (
                        <Button type={'button'} className="govuk-!-margin-left-4" buttonType="secondary" title={t('cancel')} onClick={cancel}>
                            {t('cancel')}
                        </Button>
                    )}
                </form>
            </QueryGuard>
        </>
    );
}

CommentForm.defaultProps = {
    readonly: false,
    parentId: null
};
