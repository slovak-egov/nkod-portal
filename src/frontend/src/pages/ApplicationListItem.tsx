import { Fragment } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { useUserPermissions } from '../client';
import CommentButton from '../components/CommentButton';
import GridColumn from '../components/GridColumn';
import GridRow from '../components/GridRow';
import LikeButton from '../components/LikeButton';
import { Application } from '../interface/cms.interface';

type Props = {
    app: Application;
    isLast: boolean;
    editable?: boolean;
};

const ApplicationListItem = (props: Props) => {
    const { t } = useTranslation();
    const { isLogged, isSuperAdmin, isMine } = useUserPermissions();
    const { app, isLast, editable } = props;

    const showEdit = editable && (isSuperAdmin || (isLogged && isMine(app.userId)));

    return (
        <Fragment key={app.id}>
            <GridRow data-testid="sr-result">
                <GridColumn widthUnits={1} totalUnits={1}>
                    <GridRow>
                        <GridColumn widthUnits={3} totalUnits={4}>
                            <Link to={'/aplikacia/' + app.id} className="idsk-card-title govuk-link">
                                {app.title}
                            </Link>
                        </GridColumn>
                        <GridColumn widthUnits={1} totalUnits={4} flexEnd>
                            {showEdit && (
                                <Link to={`/aplikacia/${app.id}/upravit`} className="idsk-card-title govuk-link">
                                    {t('common.edit')}
                                </Link>
                            )}
                            <LikeButton count={app.likeCount} contentId={app.id} url={`applications/likes`} />
                            <Link to={`/aplikacia/${app.id}/komentare`} className="no-link">
                                <CommentButton count={app.commentCount} />
                            </Link>
                        </GridColumn>
                    </GridRow>
                </GridColumn>
                {app.description && (
                    <GridColumn widthUnits={1} totalUnits={1}>
                        <div
                            style={{
                                WebkitLineClamp: 3,
                                WebkitBoxOrient: 'vertical',
                                overflow: 'hidden',
                                textOverflow: 'ellipsis',
                                display: '-webkit-box'
                            }}
                        >
                            {app.description}
                        </div>
                    </GridColumn>
                )}
                {app.type && (
                    <>
                        <GridColumn widthUnits={1} totalUnits={2}>
                            <span style={{ color: '#000', fontWeight: 'bold' }}>{t(`codelists.applicationType.${app.type}`)}</span>
                        </GridColumn>
                    </>
                )}
            </GridRow>
            {!isLast ? <hr className="idsk-search-results__card__separator" /> : null}
        </Fragment>
    );
};

export default ApplicationListItem;

ApplicationListItem.defaultProps = {
    editable: true
};
